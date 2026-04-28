using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Data.Configurations;
using PrepDiplomacia.Infrastructure.Identity;

namespace PrepDiplomacia.Infrastructure.Data;

/// <summary>
/// DbContext principal que integra Identity y las entidades de dominio.
///
/// Soporta tanto SQLite (desarrollo y arranque) como SQL Server
/// (cuando el proyecto crezca). El proveedor se elige en Program.cs
/// vía configuración (DatabaseProvider = "Sqlite" | "SqlServer").
/// </summary>
public class AppDbContext : IdentityDbContext<UsuarioAplicacion>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Blog ────────────────────────────────────────────────────
    public DbSet<PostBlog> Posts => Set<PostBlog>();
    public DbSet<CategoriaBlog> Categorias => Set<CategoriaBlog>();
    public DbSet<TagBlog> Tags => Set<TagBlog>();
    public DbSet<PostBlogTag> PostTags => Set<PostBlogTag>();
    public DbSet<ComentarioBlog> Comentarios => Set<ComentarioBlog>();

    // ── Contenido editable ──────────────────────────────────────
    public DbSet<BloqueContenido> BloquesContenido => Set<BloqueContenido>();

    // ── Newsletter / Contacto ───────────────────────────────────
    public DbSet<SuscriptorNewsletter> Suscriptores => Set<SuscriptorNewsletter>();
    public DbSet<MensajeContacto> Mensajes => Set<MensajeContacto>();

    // ── Curso / Pagos ───────────────────────────────────────────
    public DbSet<PlanCurso> Planes => Set<PlanCurso>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<EventoStripeProcesado> EventosStripe => Set<EventoStripeProcesado>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Renombrar tablas de Identity a un esquema más limpio (sin AspNet*).
        builder.Entity<UsuarioAplicacion>().ToTable("Usuarios");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UsuarioRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UsuarioClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UsuarioLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UsuarioTokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RolClaims");

        // Aplicar todas las configuraciones (IEntityTypeConfiguration) del assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(PostBlogConfiguration).Assembly);
    }

    /// <summary>
    /// Override de SaveChangesAsync para auto-poblar FechaActualizacion
    /// en cualquier EntidadBase modificada.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var modificadas = ChangeTracker.Entries<Domain.Common.EntidadBase>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modificadas)
            entry.Entity.FechaActualizacion = DateTime.UtcNow;

        return base.SaveChangesAsync(cancellationToken);
    }
}
