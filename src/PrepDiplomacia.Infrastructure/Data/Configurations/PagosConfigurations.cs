using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrepDiplomacia.Domain.Entities;

namespace PrepDiplomacia.Infrastructure.Data.Configurations;

public class PlanCursoConfiguration : IEntityTypeConfiguration<PlanCurso>
{
    public void Configure(EntityTypeBuilder<PlanCurso> b)
    {
        b.ToTable("Planes");
        b.HasIndex(x => x.Codigo).IsUnique();
        // SQLite no tiene tipo decimal nativo: forzamos almacenamiento como TEXT con precisión.
        b.Property(x => x.PrecioTotal).HasConversion<double>();
    }
}

public class InscripcionConfiguration : IEntityTypeConfiguration<Inscripcion>
{
    public void Configure(EntityTypeBuilder<Inscripcion> b)
    {
        b.ToTable("Inscripciones");
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.Estado);
        b.HasIndex(x => x.UsuarioId);

        b.HasOne(x => x.PlanCurso)
         .WithMany(p => p.Inscripciones)
         .HasForeignKey(x => x.PlanCursoId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> b)
    {
        b.ToTable("Pagos");
        b.HasIndex(x => x.StripeSessionId);
        b.HasIndex(x => x.StripePaymentIntentId);
        b.HasIndex(x => x.StripeSubscriptionId);
        b.Property(x => x.Monto).HasConversion<double>();

        b.HasOne(x => x.Inscripcion)
         .WithMany(i => i.Pagos)
         .HasForeignKey(x => x.InscripcionId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventoStripeProcesadoConfiguration : IEntityTypeConfiguration<EventoStripeProcesado>
{
    public void Configure(EntityTypeBuilder<EventoStripeProcesado> b)
    {
        b.ToTable("EventosStripe");
        b.HasIndex(x => x.EventoId).IsUnique();
    }
}
