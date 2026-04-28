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

// EF Core: SQLite por defecto; cambiar a SqlServer poniendo DatabaseProvider en config.
var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=App_Data/prepdiplomacia.db";

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        opt.UseSqlServer(connectionString);
    else
        opt.UseSqlite(connectionString);
});

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

// Asegurar que existe la carpeta App_Data para la base SQLite.
var appDataPath = Path.Combine(app.Environment.ContentRootPath, "App_Data");
if (!Directory.Exists(appDataPath))
{
    Directory.CreateDirectory(appDataPath);
}

// ── Seed de datos iniciales ─────────────────────────────────────────────────
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
