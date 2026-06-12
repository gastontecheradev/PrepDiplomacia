using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Domain.Enums;
using PrepDiplomacia.Infrastructure.Identity;

namespace PrepDiplomacia.Infrastructure.Data.Seed;

/// <summary>
/// Datos iniciales del sistema. Se ejecuta al arrancar la aplicación
/// (idempotente: solo agrega lo que no existe).
///
/// 1) Crea roles Admin y Alumno.
/// 2) Crea el usuario admin (Carolina) con credenciales de configuración.
/// 3) Pobla los bloques de contenido editable con los textos por defecto del sitio.
/// 4) Crea un plan de curso de ejemplo si no hay ninguno.
/// </summary>
public static class SeedInicial
{
    public static async Task EjecutarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db          = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<UsuarioAplicacion>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var config      = sp.GetRequiredService<IConfiguration>();
        var logger      = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager, config, logger);
        await SeedBloquesContenidoAsync(db);
        await SeedPlanesEjemploAsync(db);
        await SeedCategoriasEjemploAsync(db);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var rol in new[] { RolesSistema.Admin, RolesSistema.Alumno })
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }
    }

    private static async Task SeedAdminAsync(
    UserManager<UsuarioAplicacion> userManager,
    IConfiguration config,
    ILogger logger)
    {
        var email = config["Admin:Email"] ?? "prepdiplomaciauy@gmail.com";
        var nombreCompleto = config["Admin:NombreCompleto"] ?? "Carolina Techera";
        var password = config["Admin:Password"];

        // Si ya existe el admin, no hacemos nada (idempotente).
        var existente = await userManager.FindByEmailAsync(email);
        if (existente is not null) return;

        // La contraseña DEBE venir de configuración (user-secrets en dev,
        // variables de entorno / App Service settings en producción).
        // Si no está configurada, NO creamos un admin con contraseña por defecto.
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogError(
                "No se creó el usuario admin: falta 'Admin:Password' en la configuración. " +
                "Cargala con user-secrets (dev) o variables de entorno (producción).");
            return;
        }

        var admin = new UsuarioAplicacion
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = nombreCompleto,
            TieneAccesoArea = true
        };

        var resultado = await userManager.CreateAsync(admin, password);
        if (!resultado.Succeeded)
        {
            logger.LogError("Error al crear admin: {Errores}",
                string.Join(", ", resultado.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, RolesSistema.Admin);
        logger.LogInformation("Admin creado: {Email}. Cambiar contraseña en primer login.", email);
    }

    /// <summary>
    /// Inserta los bloques de contenido predefinidos del sitio si no existen.
    /// Cada bloque tiene una clave única que las vistas Razor consultan.
    /// </summary>
    private static async Task SeedBloquesContenidoAsync(AppDbContext db)
    {
        if (await db.BloquesContenido.AnyAsync()) return;

        var bloques = new List<BloqueContenido>
        {
            // ── HOME / HERO ──
            new() { Clave = "home.hero.eyebrow",  Etiqueta = "Etiqueta superior del hero",
                    Seccion = "Inicio", Tipo = "texto", Orden = 1,
                    Valor = "Concurso MRREE 2027" },
            new() { Clave = "home.hero.titulo1",  Etiqueta = "Título 1 del hero",
                    Seccion = "Inicio", Tipo = "texto", Orden = 2,
                    Valor = "Prep" },
            new() { Clave = "home.hero.titulo2",  Etiqueta = "Título 2 del hero (en cursiva)",
                    Seccion = "Inicio", Tipo = "texto", Orden = 3,
                    Valor = "Diplomacia" },
            new() { Clave = "home.hero.descripcion", Etiqueta = "Descripción del hero",
                    Seccion = "Inicio", Tipo = "parrafo", Orden = 4,
                    Valor = "Formación especializada para el Concurso de Ingreso al Servicio Exterior de Uruguay. Preparación de excelencia, construida desde la experiencia diplomática real." },

            // ── HOME / POR QUÉ ──
            new() { Clave = "home.porque.eyebrow",  Etiqueta = "Etiqueta sección por qué",
                    Seccion = "Inicio", Tipo = "texto", Orden = 10,
                    Valor = "Por qué elegirnos" },
            new() { Clave = "home.porque.titulo",   Etiqueta = "Título sección por qué",
                    Seccion = "Inicio", Tipo = "texto", Orden = 11,
                    Valor = "Por qué Prep Diplomacia" },
            new() { Clave = "home.porque.intro",    Etiqueta = "Intro sección por qué",
                    Seccion = "Inicio", Tipo = "parrafo", Orden = 12,
                    Valor = "La mayoría de los candidatos llegan al concurso sin saber con precisión en qué consiste cada instancia, qué evalúa el tribunal ni cómo distribuir el esfuerzo." },

            new() { Clave = "home.porque.card1.titulo", Etiqueta = "Card 1 — Título",
                    Seccion = "Inicio", Tipo = "texto", Orden = 13,
                    Valor = "Acompañamiento completo" },
            new() { Clave = "home.porque.card1.texto",  Etiqueta = "Card 1 — Texto",
                    Seccion = "Inicio", Tipo = "parrafo", Orden = 14,
                    Valor = "Preparación de las seis pruebas del concurso, con simulacros corregidos y devolución personalizada en cada instancia." },

            new() { Clave = "home.porque.card2.titulo", Etiqueta = "Card 2 — Título",
                    Seccion = "Inicio", Tipo = "texto", Orden = 15,
                    Valor = "Dirección con experiencia real" },
            new() { Clave = "home.porque.card2.texto",  Etiqueta = "Card 2 — Texto",
                    Seccion = "Inicio", Tipo = "parrafo", Orden = 16,
                    Valor = "Coordinación a cargo de Carolina Techera, ex funcionaria del Servicio Exterior del Uruguay y ex Cónsul en París." },

            new() { Clave = "home.porque.card3.titulo", Etiqueta = "Card 3 — Título",
                    Seccion = "Inicio", Tipo = "texto", Orden = 17,
                    Valor = "Metodología de aula invertida" },
            new() { Clave = "home.porque.card3.texto",  Etiqueta = "Card 3 — Texto",
                    Seccion = "Inicio", Tipo = "parrafo", Orden = 18,
                    Valor = "El estudiante trabaja el material teórico antes de cada clase. Las sesiones sincrónicas profundizan y ejercitan." },

            // ── HOME / CTA FINAL ──
            new() { Clave = "home.cta.titulo", Etiqueta = "Título CTA final del home",
                    Seccion = "Inicio", Tipo = "texto", Orden = 30,
                    Valor = "Concurso MRREE 2027 — Inicio del curso en agosto 2026" },
            new() { Clave = "home.cta.texto", Etiqueta = "Texto CTA final del home",
                    Seccion = "Inicio", Tipo = "parrafo", Orden = 31,
                    Valor = " Las pre inscripciones abren en junio de 2026. Los cupos son limitados. Dejanos tus datos para recibir toda la información del programa antes del lanzamiento." },

            // ── SOBRE ──
            new() { Clave = "sobre.intro", Etiqueta = "Intro Sobre Prep",
                    Seccion = "Sobre Prep", Tipo = "parrafo", Orden = 1,
                    Valor = "Internacionalista, consultora y ex diplomática del Servicio Exterior del Uruguay y ex Cónsul en París, es la fundadora y coordinadora académica de Prep Diplomacia." },
            new() { Clave = "sobre.trayectoria", Etiqueta = "Trayectoria",
                    Seccion = "Sobre Prep", Tipo = "html", Orden = 2,
                    Valor = "<p>Ejercí como funcionaria del Servicio Exterior de la República y cumplí funciones consulares en París, lo que me permitió conocer de primera mano el funcionamiento real de la diplomacia uruguaya, sus exigencias operativas y el perfil que la institución busca en quienes ingresan a la carrera.</p><p>A esa experiencia en misión se suma una trayectoria en investigación académica y consultoría especializada en relaciones internacionales y diplomacia.</p>" },
            new() { Clave = "sobre.por_que_existe", Etiqueta = "Por qué existe",
                    Seccion = "Sobre Prep", Tipo = "html", Orden = 3,
                    Valor = "<p>El Concurso de Ingreso al Servicio Exterior es uno de los procesos de selección más exigentes del Estado uruguayo. Su dificultad no radica únicamente en la amplitud del temario, sino en la diversidad de competencias que evalúa.</p>" },
            new() { Clave = "sobre.enfoque", Etiqueta = "Enfoque pedagógico",
                    Seccion = "Sobre Prep", Tipo = "html", Orden = 4,
                    Valor = "<p>El programa trabaja con bibliografía actualizada y conocimiento de coyuntura internacional, a través del modelo de aula invertida.</p>" },

            // ── PROGRAMA ──
            new() { Clave = "programa.intro", Etiqueta = "Intro Programa",
                    Seccion = "Programa", Tipo = "parrafo", Orden = 1,
                    Valor = "Seis meses de trabajo académico con cursado sincrónico, materiales propios y seguimiento individual en todas las instancias del concurso." },

            // ── CONTACTO ──
            new() { Clave = "contacto.email", Etiqueta = "Email de contacto público",
                    Seccion = "Contacto", Tipo = "texto", Orden = 1,
                    Valor = "prepdiplomaciauy@gmail.com" },
            new() { Clave = "contacto.instagram", Etiqueta = "Instagram",
                    Seccion = "Contacto", Tipo = "texto", Orden = 2,
                    Valor = "@prepdiplomaciauy" },
        };

        await db.BloquesContenido.AddRangeAsync(bloques);
        await db.SaveChangesAsync();
    }

    private static async Task SeedPlanesEjemploAsync(AppDbContext db)
    {
        if (await db.Planes.AnyAsync()) return;

        var planes = new List<PlanCurso>
        {
            new()
            {
                Nombre = "Plan Básico",
                Codigo = "BASICO_2027",
                Descripcion = "Acceso a las clases sincrónicas y materiales del programa.",
                PrecioTotal = 800m,
                Moneda = "usd",
                ModalidadPago = ModalidadPago.PagoUnico,
                CantidadCuotas = 1,
                Caracteristicas = "Clases sincrónicas\nMateriales del programa\nAcceso al área de alumnos",
                Activo = true,
                Orden = 1,
                Destacado = false
            },
            new()
            {
                Nombre = "Plan Completo",
                Codigo = "COMPLETO_2027",
                Descripcion = "Programa completo con simulacros corregidos y devolución personalizada.",
                PrecioTotal = 1400m,
                Moneda = "usd",
                ModalidadPago = ModalidadPago.PagoUnico,
                CantidadCuotas = 1,
                Caracteristicas = "Todo lo del Plan Básico\nSimulacros corregidos\nDevolución individualizada\nSimulación de prueba oral\nSimulación de entrevista",
                Activo = true,
                Orden = 2,
                Destacado = true
            },
            new()
            {
                Nombre = "Plan Completo en Cuotas",
                Codigo = "COMPLETO_2027_CUOTAS",
                Descripcion = "Plan Completo dividido en 6 cuotas mensuales.",
                PrecioTotal = 1500m,
                Moneda = "usd",
                ModalidadPago = ModalidadPago.Cuotas,
                CantidadCuotas = 6,
                Caracteristicas = "Mismas prestaciones que el Plan Completo\nPagos mensuales automáticos\nCancelable según términos",
                Activo = true,
                Orden = 3,
                Destacado = false
            }
        };

        await db.Planes.AddRangeAsync(planes);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategoriasEjemploAsync(AppDbContext db)
    {
        if (await db.Categorias.AnyAsync()) return;

        var categorias = new List<CategoriaBlog>
        {
            new() { Nombre = "Concurso",            Slug = "concurso",            Descripcion = "Información sobre el Concurso de Ingreso al Servicio Exterior.", Color = "#0C3F67" },
            new() { Nombre = "Política Internacional", Slug = "politica-internacional", Descripcion = "Análisis de coyuntura.", Color = "#F3BD2D" },
            new() { Nombre = "Derecho Internacional", Slug = "derecho-internacional",   Descripcion = "Notas sobre DIP y DIPriv.", Color = "#0C3F67" },
            new() { Nombre = "Carrera Diplomática",   Slug = "carrera-diplomatica",     Descripcion = "Vida y formación en el Servicio Exterior.", Color = "#F3BD2D" }
        };

        await db.Categorias.AddRangeAsync(categorias);
        await db.SaveChangesAsync();
    }
}
