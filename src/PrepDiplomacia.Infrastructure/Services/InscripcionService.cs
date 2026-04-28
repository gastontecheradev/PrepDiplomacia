using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Domain.Enums;
using PrepDiplomacia.Infrastructure.Data;
using PrepDiplomacia.Infrastructure.Identity;

namespace PrepDiplomacia.Infrastructure.Services;

public interface IInscripcionService
{
    /// <summary>Crea o reutiliza una inscripción "PagoPendiente" para iniciar Checkout.</summary>
    Task<Inscripcion> CrearOActualizarPendienteAsync(
        string nombre, string email, string? telefono,
        string? formacion, string? consulta,
        int planCursoId, ModalidadPago modalidad);

    /// <summary>Activa una inscripción tras pago confirmado, crea/asocia usuario Identity y le asigna rol Alumno.</summary>
    Task<bool> ActivarPostPagoAsync(int inscripcionId, string? stripeCustomerId);

    Task<Inscripcion?> ObtenerPorIdAsync(int id);
    Task<List<Inscripcion>> ListarTodasAsync();
}

public class InscripcionService : IInscripcionService
{
    private readonly AppDbContext _db;
    private readonly UserManager<UsuarioAplicacion> _userManager;
    private readonly ILogger<InscripcionService> _logger;

    public InscripcionService(
        AppDbContext db,
        UserManager<UsuarioAplicacion> userManager,
        ILogger<InscripcionService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Inscripcion> CrearOActualizarPendienteAsync(
        string nombre, string email, string? telefono,
        string? formacion, string? consulta,
        int planCursoId, ModalidadPago modalidad)
    {
        // Si ya existe una inscripción del mismo email para el mismo plan en estado
        // EnListaDeEspera o PagoPendiente, la reutilizamos para no duplicar.
        var existente = await _db.Inscripciones
            .Where(i => i.Email == email
                     && i.PlanCursoId == planCursoId
                     && (i.Estado == EstadoInscripcion.EnListaDeEspera ||
                         i.Estado == EstadoInscripcion.PagoPendiente))
            .OrderByDescending(i => i.FechaCreacion)
            .FirstOrDefaultAsync();

        if (existente is not null)
        {
            existente.NombreCompleto = nombre;
            existente.Telefono = telefono;
            existente.FormacionAcademica = formacion;
            existente.ConsultaAdicional = consulta;
            existente.ModalidadElegida = modalidad;
            existente.Estado = EstadoInscripcion.PagoPendiente;
            await _db.SaveChangesAsync();
            return existente;
        }

        var nueva = new Inscripcion
        {
            NombreCompleto = nombre,
            Email = email,
            Telefono = telefono,
            FormacionAcademica = formacion,
            ConsultaAdicional = consulta,
            PlanCursoId = planCursoId,
            ModalidadElegida = modalidad,
            Estado = EstadoInscripcion.PagoPendiente
        };
        _db.Inscripciones.Add(nueva);
        await _db.SaveChangesAsync();
        return nueva;
    }

    public async Task<bool> ActivarPostPagoAsync(int inscripcionId, string? stripeCustomerId)
    {
        var inscripcion = await _db.Inscripciones
            .Include(i => i.PlanCurso)
            .FirstOrDefaultAsync(i => i.Id == inscripcionId);
        if (inscripcion is null) return false;

        // Crear o reutilizar usuario Identity con el email de la inscripción.
        var usuario = await _userManager.FindByEmailAsync(inscripcion.Email);
        if (usuario is null)
        {
            usuario = new UsuarioAplicacion
            {
                UserName = inscripcion.Email,
                Email = inscripcion.Email,
                EmailConfirmed = true,
                NombreCompleto = inscripcion.NombreCompleto,
                TieneAccesoArea = true
            };

            // Generar contraseña temporal aleatoria. El usuario la cambiará vía "Olvidé mi contraseña".
            var passwordTemp = GenerarPasswordSegura();
            var creacion = await _userManager.CreateAsync(usuario, passwordTemp);
            if (!creacion.Succeeded)
            {
                _logger.LogError("No se pudo crear usuario para inscripción {Id}: {Errores}",
                    inscripcionId, string.Join(", ", creacion.Errors.Select(e => e.Description)));
                return false;
            }

            // En un siguiente paso se le envía un email con un link "Reset password"
            // para que defina su propia contraseña. Esto se delega al EmailService desde el caller.
        }
        else
        {
            usuario.TieneAccesoArea = true;
            await _userManager.UpdateAsync(usuario);
        }

        if (!await _userManager.IsInRoleAsync(usuario, RolesSistema.Alumno))
            await _userManager.AddToRoleAsync(usuario, RolesSistema.Alumno);

        inscripcion.UsuarioId = usuario.Id;
        inscripcion.Estado = EstadoInscripcion.Activa;
        inscripcion.FechaActivacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Inscripción {Id} activada para usuario {UserId}", inscripcionId, usuario.Id);
        return true;
    }

    public Task<Inscripcion?> ObtenerPorIdAsync(int id) =>
        _db.Inscripciones
            .Include(i => i.PlanCurso)
            .Include(i => i.Pagos)
            .FirstOrDefaultAsync(i => i.Id == id);

    public Task<List<Inscripcion>> ListarTodasAsync() =>
        _db.Inscripciones
            .Include(i => i.PlanCurso)
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();

    /// <summary>Contraseña temporal segura: 16 caracteres alfanuméricos + símbolos.</summary>
    private static string GenerarPasswordSegura()
    {
        const string mayus  = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minus  = "abcdefghijkmnpqrstuvwxyz";
        const string nums   = "23456789";
        const string simbol = "!@#$%&*";
        var rnd = new Random();
        var chars = new List<char>
        {
            mayus[rnd.Next(mayus.Length)],
            minus[rnd.Next(minus.Length)],
            nums[rnd.Next(nums.Length)],
            simbol[rnd.Next(simbol.Length)]
        };
        var pool = mayus + minus + nums + simbol;
        for (int i = 0; i < 12; i++)
            chars.Add(pool[rnd.Next(pool.Length)]);
        return new string(chars.OrderBy(_ => rnd.Next()).ToArray());
    }
}
