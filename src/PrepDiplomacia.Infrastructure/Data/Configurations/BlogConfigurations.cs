using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrepDiplomacia.Domain.Entities;

namespace PrepDiplomacia.Infrastructure.Data.Configurations;

public class PostBlogConfiguration : IEntityTypeConfiguration<PostBlog>
{
    public void Configure(EntityTypeBuilder<PostBlog> b)
    {
        b.ToTable("Posts");
        b.HasIndex(p => p.Slug).IsUnique();
        b.HasIndex(p => p.Estado);
        b.HasIndex(p => p.FechaPublicacion);

        b.HasOne(p => p.Categoria)
         .WithMany(c => c.Posts)
         .HasForeignKey(p => p.CategoriaId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CategoriaBlogConfiguration : IEntityTypeConfiguration<CategoriaBlog>
{
    public void Configure(EntityTypeBuilder<CategoriaBlog> b)
    {
        b.ToTable("Categorias");
        b.HasIndex(c => c.Slug).IsUnique();
        b.HasIndex(c => c.Nombre).IsUnique();
    }
}

public class TagBlogConfiguration : IEntityTypeConfiguration<TagBlog>
{
    public void Configure(EntityTypeBuilder<TagBlog> b)
    {
        b.ToTable("Tags");
        b.HasIndex(t => t.Slug).IsUnique();
        b.HasIndex(t => t.Nombre).IsUnique();
    }
}

public class PostBlogTagConfiguration : IEntityTypeConfiguration<PostBlogTag>
{
    public void Configure(EntityTypeBuilder<PostBlogTag> b)
    {
        b.ToTable("PostTags");
        b.HasKey(pt => new { pt.PostBlogId, pt.TagBlogId });
        b.HasOne(pt => pt.PostBlog)
         .WithMany(p => p.PostTags)
         .HasForeignKey(pt => pt.PostBlogId)
         .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(pt => pt.TagBlog)
         .WithMany(t => t.PostTags)
         .HasForeignKey(pt => pt.TagBlogId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ComentarioBlogConfiguration : IEntityTypeConfiguration<ComentarioBlog>
{
    public void Configure(EntityTypeBuilder<ComentarioBlog> b)
    {
        b.ToTable("Comentarios");
        b.HasIndex(c => c.PostBlogId);
        b.HasIndex(c => c.Aprobado);
        b.HasOne(c => c.PostBlog)
         .WithMany(p => p.Comentarios)
         .HasForeignKey(c => c.PostBlogId)
         .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.ComentarioPadre)
         .WithMany(c => c.Respuestas)
         .HasForeignKey(c => c.ComentarioPadreId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
