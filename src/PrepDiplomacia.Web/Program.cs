using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrepDiplomacia.Infrastructure.Data;
using PrepDiplomacia.Infrastructure.Data.Seed;
using PrepDiplomacia.Infrastructure.Email;
using PrepDiplomacia.Infrastructure.Identity;
using PrepDiplomacia.Infrastructure.Newsletter;
using PrepDiplomacia.Infrastructure.Payments;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// ── Configuración de servicios ──────────────────────────────────────────────

// EF Core sobre SQL Server: LocalDB en desarrollo, Azure SQL Database en producción.
// La cadena se resuelve en este orden: variable de entorno / App Service settings
// (ConnectionStrings__Default) > User Secrets > appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
{
    // Preferimos fallar al arrancar, con un mensaje claro, antes que quedar
    // en un estado a medias sin base de datos.
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:Default'. " +
        "En desarrollo se define en appsettings.json o en User Secrets; " +
        "en Azure App Service, como Application Setting 'ConnectionStrings__Default'.");
}

// Salvaguarda: si se publica sin configurar la cadena real, la app apuntaría a
// una base local inexistente en el servidor. Mejor detectarlo en el arranque.
if (!builder.Environment.IsDevelopment() &&
    connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "La cadena de conexión apunta a LocalDB fuera del entorno de Desarrollo. " +
        "Configurá 'ConnectionStrings__Default' con la cadena de Azure SQL Database.");
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(connectionString, sql =>
    {
        // Azure SQL corta conexiones de forma transitoria (throttling, failover,
        // reanudación tras pausa). Sin reintentos esos cortes llegan como error 500.
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);

        // El primer arranque tras un período de inactividad puede ser lento
        // en los niveles Basic/Serverless.
        sql.CommandTimeout(60);
    }));

// Identity con UI mínima propia (no usamos las páginas Razor de Identity, todo es MVC).
builder.Services
    .AddIdentity<UsuarioAplicacion, IdentityRole>(opt =>
    {
        opt.Password.RequireDigit           = true;
        opt.Password.RequireLowercase       = true;
        opt.Password.RequireUppercase       = true;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequiredLength         = 8;
        opt.SignIn.RequireConfirmedEmail    = false; // Carolina activa cuentas manualmente; los alumnos vía pago.
        opt.User.RequireUniqueEmail         = true;
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath        = "/cuenta/login";
    opt.LogoutPath       = "/cuenta/logout";
    opt.AccessDeniedPath = "/cuenta/sin-acceso";
    opt.ExpireTimeSpan   = TimeSpan.FromDays(30);
    opt.SlidingExpiration = true;
    opt.Cookie.Name      = "PrepDiplomacia.Auth";
});

// ── Servicios de negocio ────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.Configure<MailchimpOptions>(builder.Configuration.GetSection(MailchimpOptions.SectionName));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddHttpClient<INewsletterService, MailchimpService>();
builder.Services.AddScoped<IFileStorageService, FileStorageLocalService>();
builder.Services.AddScoped<IContenidoService, ContenidoService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IInscripcionService, InscripcionService>();
builder.Services.AddScoped<ISuscriptorService, SuscriptorService>();
builder.Services.AddScoped<IMensajeService, MensajeService>();
builder.Services.AddScoped<IPlanCursoService, PlanCursoService>();

// MVC.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// HttpContext en vistas / servicios (para IPs, etc.).
builder.Services.AddHttpContextAccessor();

// ── Build y pipeline ────────────────────────────────────────────────────────
var app = builder.Build();

// ── Cultura: fechas y nombres de meses en español ───────────────────────────
// Sin esto, ToString("MMMM") devuelve los meses en inglés ("June").
// Conservamos el formato numérico invariante (separador decimal ".") para no
// alterar el binding de decimales en formularios ni la integración con Stripe.
var culturaEs = (CultureInfo)CultureInfo.GetCultureInfo("es-UY").Clone();
culturaEs.NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
CultureInfo.DefaultThreadCurrentCulture   = culturaEs;
CultureInfo.DefaultThreadCurrentUICulture = culturaEs;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Área Admin — montada en /admin
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Rutas amigables explícitas para SEO.
app.MapControllerRoute(name: "blog-detalle",
    pattern: "blog/{slug}", defaults: new { controller = "Blog", action = "Detalle" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Identity utiliza algunas páginas Razor por convención.

// ── Seed de datos iniciales ─────────────────────────────────────────────────
// SeedInicial ejecuta db.Database.MigrateAsync(): al arrancar contra una base
// vacía crea el esquema completo y siembra roles, admin, contenidos y planes.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await SeedInicial.EjecutarAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogError(ex, "Error ejecutando seed inicial");
    }
}

app.Run();
