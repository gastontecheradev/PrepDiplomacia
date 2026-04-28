using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Data;
using PrepDiplomacia.Infrastructure.Newsletter;

namespace PrepDiplomacia.Infrastructure.Services;

public interface ISuscriptorService
{
    /// <summary>
    /// Suscribe (o resucita una suscripción) en BD local y sincroniza con Mailchimp.
    /// Idempotente: si el email ya existe activo, retorna éxito sin duplicar.
    /// </summary>
    Task<(bool ok, string mensaje)> SuscribirAsync(string email, string? nombre,
                                                    string? origen, string? ip);

    Task<List<SuscriptorNewsletter>> ListarTodosAsync();
    Task<bool> DarDeBajaAsync(int id);
    Task<bool> EliminarAsync(int id);
}

public class SuscriptorService : ISuscriptorService
{
    private readonly AppDbContext _db;
    private readonly INewsletterService _newsletter;
    private readonly ILogger<SuscriptorService> _logger;

    public SuscriptorService(AppDbContext db, INewsletterService newsletter, ILogger<SuscriptorService> logger)
    {
        _db = db;
        _newsletter = newsletter;
        _logger = logger;
    }

    public async Task<(bool ok, string mensaje)> SuscribirAsync(
        string email, string? nombre, string? origen, string? ip)
    {
        email = email.Trim().ToLowerInvariant();

        var existente = await _db.Suscriptores.FirstOrDefaultAsync(s => s.Email == email);

        if (existente is not null && !existente.DadoDeBaja)
        {
            return (true, "Este correo ya está suscripto a las novedades.");
        }

        if (existente is not null && existente.DadoDeBaja)
        {
            existente.DadoDeBaja = false;
            existente.FechaBaja = null;
            existente.Nombre = nombre ?? existente.Nombre;
            await _db.SaveChangesAsync();
        }
        else
        {
            existente = new SuscriptorNewsletter
            {
                Email = email,
                Nombre = nombre,
                Origen = origen,
                IpSuscripcion = ip
            };
            _db.Suscriptores.Add(existente);
            await _db.SaveChangesAsync();
        }

        // Sincronizar con Mailchimp (no-bloqueante semánticamente: si falla,
        // la suscripción local ya quedó guardada y Carolina puede reintentar manualmente).
        var resultado = await _newsletter.SuscribirAsync(email, nombre);
        if (resultado.Exito)
        {
            existente.MailchimpId = resultado.MailchimpId;
            await _db.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("Mailchimp falló para {Email}: {Error}", email, resultado.Error);
        }

        return (true, "Suscripción registrada. ¡Gracias!");
    }

    public Task<List<SuscriptorNewsletter>> ListarTodosAsync() =>
        _db.Suscriptores.OrderByDescending(s => s.FechaCreacion).ToListAsync();

    public async Task<bool> DarDeBajaAsync(int id)
    {
        var s = await _db.Suscriptores.FindAsync(id);
        if (s is null) return false;
        s.DadoDeBaja = true;
        s.FechaBaja = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var s = await _db.Suscriptores.FindAsync(id);
        if (s is null) return false;
        _db.Suscriptores.Remove(s);
        await _db.SaveChangesAsync();
        return true;
    }
}
