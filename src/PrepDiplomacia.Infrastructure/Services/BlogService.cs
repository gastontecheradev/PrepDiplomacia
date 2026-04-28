using Microsoft.EntityFrameworkCore;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Domain.Enums;
using PrepDiplomacia.Infrastructure.Data;

namespace PrepDiplomacia.Infrastructure.Services;

public interface IBlogService
{
    /// <summary>Posts publicados, paginados y filtrables. Para el sitio público.</summary>
    Task<(List<PostBlog> posts, int total)> ListarPublicadosAsync(
        int pagina = 1, int tamanioPagina = 9,
        int? categoriaId = null, int? tagId = null, string? busqueda = null);

    /// <summary>Post publicado por slug. Incluye categoría, tags y comentarios aprobados.</summary>
    Task<PostBlog?> ObtenerPublicadoPorSlugAsync(string slug);

    /// <summary>Posts más recientes (sidebar / footer del blog).</summary>
    Task<List<PostBlog>> ObtenerRecientesAsync(int cantidad = 5);

    /// <summary>Para el listado del admin.</summary>
    Task<List<PostBlog>> ListarTodosAsync();

    Task<PostBlog?> ObtenerPorIdAsync(int id);

    Task<PostBlog> CrearAsync(PostBlog post, int[] tagIds);

    Task<bool> ActualizarAsync(PostBlog post, int[] tagIds);

    Task<bool> EliminarAsync(int id);

    Task IncrementarVistasAsync(int postId);

    Task<List<CategoriaBlog>> ListarCategoriasAsync();
    Task<List<TagBlog>> ListarTagsAsync();

    Task<ComentarioBlog> CrearComentarioAsync(ComentarioBlog comentario);
    Task<bool> AprobarComentarioAsync(int comentarioId);
    Task<bool> EliminarComentarioAsync(int comentarioId);
    Task<List<ComentarioBlog>> ListarComentariosPendientesAsync();
}

public class BlogService : IBlogService
{
    private readonly AppDbContext _db;

    public BlogService(AppDbContext db) => _db = db;

    public async Task<(List<PostBlog> posts, int total)> ListarPublicadosAsync(
        int pagina = 1, int tamanioPagina = 9,
        int? categoriaId = null, int? tagId = null, string? busqueda = null)
    {
        if (pagina < 1) pagina = 1;
        if (tamanioPagina < 1) tamanioPagina = 9;

        var query = _db.Posts
            .Include(p => p.Categoria)
            .Include(p => p.PostTags).ThenInclude(pt => pt.TagBlog)
            .Where(p => p.Estado == EstadoPublicacion.Publicado
                     && p.FechaPublicacion <= DateTime.UtcNow)
            .AsQueryable();

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        if (tagId.HasValue)
            query = query.Where(p => p.PostTags.Any(pt => pt.TagBlogId == tagId.Value));

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var b = busqueda.Trim().ToLower();
            query = query.Where(p =>
                p.Titulo.ToLower().Contains(b) ||
                p.Resumen.ToLower().Contains(b) ||
                p.Contenido.ToLower().Contains(b));
        }

        var total = await query.CountAsync();

        var posts = await query
            .OrderByDescending(p => p.FechaPublicacion)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync();

        return (posts, total);
    }

    public Task<PostBlog?> ObtenerPublicadoPorSlugAsync(string slug)
    {
        return _db.Posts
            .Include(p => p.Categoria)
            .Include(p => p.PostTags).ThenInclude(pt => pt.TagBlog)
            .Include(p => p.Comentarios.Where(c => c.Aprobado))
            .Where(p => p.Slug == slug && p.Estado == EstadoPublicacion.Publicado)
            .FirstOrDefaultAsync();
    }

    public Task<List<PostBlog>> ObtenerRecientesAsync(int cantidad = 5) =>
        _db.Posts
           .Include(p => p.Categoria)
           .Where(p => p.Estado == EstadoPublicacion.Publicado)
           .OrderByDescending(p => p.FechaPublicacion)
           .Take(cantidad).ToListAsync();

    public Task<List<PostBlog>> ListarTodosAsync() =>
        _db.Posts
           .Include(p => p.Categoria)
           .OrderByDescending(p => p.FechaCreacion)
           .ToListAsync();

    public Task<PostBlog?> ObtenerPorIdAsync(int id) =>
        _db.Posts
           .Include(p => p.Categoria)
           .Include(p => p.PostTags).ThenInclude(pt => pt.TagBlog)
           .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PostBlog> CrearAsync(PostBlog post, int[] tagIds)
    {
        post.Slug = await GenerarSlugUnicoAsync(post.Titulo);
        if (post.Estado == EstadoPublicacion.Publicado && post.FechaPublicacion is null)
            post.FechaPublicacion = DateTime.UtcNow;

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        if (tagIds is { Length: > 0 })
        {
            foreach (var tagId in tagIds.Distinct())
                _db.PostTags.Add(new PostBlogTag { PostBlogId = post.Id, TagBlogId = tagId });
            await _db.SaveChangesAsync();
        }

        return post;
    }

    public async Task<bool> ActualizarAsync(PostBlog post, int[] tagIds)
    {
        var existente = await _db.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == post.Id);
        if (existente is null) return false;

        // Si cambió el título, regenerar slug.
        if (existente.Titulo != post.Titulo)
            existente.Slug = await GenerarSlugUnicoAsync(post.Titulo, post.Id);

        existente.Titulo = post.Titulo;
        existente.Resumen = post.Resumen;
        existente.Contenido = post.Contenido;
        existente.ImagenDestacada = post.ImagenDestacada ?? existente.ImagenDestacada;
        existente.ImagenAlt = post.ImagenAlt;
        existente.YouTubeVideoId = post.YouTubeVideoId;
        existente.CategoriaId = post.CategoriaId;
        existente.MetaTitulo = post.MetaTitulo;
        existente.MetaDescripcion = post.MetaDescripcion;
        existente.ComentariosHabilitados = post.ComentariosHabilitados;

        // Si pasa de borrador a publicado y no tenía fecha, asignarla.
        if (existente.Estado != EstadoPublicacion.Publicado
            && post.Estado == EstadoPublicacion.Publicado
            && existente.FechaPublicacion is null)
        {
            existente.FechaPublicacion = DateTime.UtcNow;
        }
        existente.Estado = post.Estado;

        // Reemplazar tags.
        _db.PostTags.RemoveRange(existente.PostTags);
        if (tagIds is { Length: > 0 })
        {
            foreach (var tagId in tagIds.Distinct())
                _db.PostTags.Add(new PostBlogTag { PostBlogId = post.Id, TagBlogId = tagId });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post is null) return false;
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task IncrementarVistasAsync(int postId)
    {
        // Update directo, sin trackear, para no chocar con concurrencia.
        await _db.Posts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Vistas, p => p.Vistas + 1));
    }

    public Task<List<CategoriaBlog>> ListarCategoriasAsync() =>
        _db.Categorias.OrderBy(c => c.Nombre).ToListAsync();

    public Task<List<TagBlog>> ListarTagsAsync() =>
        _db.Tags.OrderBy(t => t.Nombre).ToListAsync();

    public async Task<ComentarioBlog> CrearComentarioAsync(ComentarioBlog comentario)
    {
        comentario.Aprobado = false; // Siempre requiere moderación.
        _db.Comentarios.Add(comentario);
        await _db.SaveChangesAsync();
        return comentario;
    }

    public async Task<bool> AprobarComentarioAsync(int comentarioId)
    {
        var c = await _db.Comentarios.FindAsync(comentarioId);
        if (c is null) return false;
        c.Aprobado = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarComentarioAsync(int comentarioId)
    {
        var c = await _db.Comentarios.FindAsync(comentarioId);
        if (c is null) return false;
        _db.Comentarios.Remove(c);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<List<ComentarioBlog>> ListarComentariosPendientesAsync() =>
        _db.Comentarios
            .Include(c => c.PostBlog)
            .Where(c => !c.Aprobado)
            .OrderBy(c => c.FechaCreacion)
            .ToListAsync();

    /// <summary>Genera un slug único, agregando -2, -3, etc. si ya existe.</summary>
    private async Task<string> GenerarSlugUnicoAsync(string titulo, int? excluirId = null)
    {
        var baseSlug = SlugHelper.Generar(titulo);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "post-" + Guid.NewGuid().ToString("N")[..6];

        var slug = baseSlug;
        var contador = 1;

        while (await _db.Posts.AnyAsync(p => p.Slug == slug && p.Id != excluirId))
        {
            contador++;
            slug = $"{baseSlug}-{contador}";
        }

        return slug;
    }
}
