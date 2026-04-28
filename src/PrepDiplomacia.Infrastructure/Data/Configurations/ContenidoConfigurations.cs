using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrepDiplomacia.Domain.Entities;

namespace PrepDiplomacia.Infrastructure.Data.Configurations;

public class BloqueContenidoConfiguration : IEntityTypeConfiguration<BloqueContenido>
{
    public void Configure(EntityTypeBuilder<BloqueContenido> b)
    {
        b.ToTable("BloquesContenido");
        b.HasIndex(x => x.Clave).IsUnique();
        b.HasIndex(x => x.Seccion);
    }
}

public class SuscriptorNewsletterConfiguration : IEntityTypeConfiguration<SuscriptorNewsletter>
{
    public void Configure(EntityTypeBuilder<SuscriptorNewsletter> b)
    {
        b.ToTable("Suscriptores");
        b.HasIndex(x => x.Email).IsUnique();
    }
}

public class MensajeContactoConfiguration : IEntityTypeConfiguration<MensajeContacto>
{
    public void Configure(EntityTypeBuilder<MensajeContacto> b)
    {
        b.ToTable("Mensajes");
        b.HasIndex(x => x.Leido);
        b.HasIndex(x => x.FechaCreacion);
    }
}
