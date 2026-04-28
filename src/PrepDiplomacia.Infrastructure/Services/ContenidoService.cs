using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Data;

namespace PrepDiplomacia.Infrastructure.Services;

public interface IContenidoService
{
    /// <summary>
    /// Devuelve el valor de un bloque editable por su clave.
    /// Si no existe en BD, retorna el fallback indicado.
    /// Cachea los resultados por 5 minutos para no golpear la BD en cada vista.
    /// </summary>
    Task<string> ObtenerAsync(string clave, string fallback = "");

    /// <summary>Devuelve todos los bloques de una sección, ordenados.</summary>
    Task<List<BloqueContenido>> ObtenerPorSeccionAsync(string seccion);

    /// <summary>Devuelve todos los bloques agrupados por sección.</summary>
    Task<Dictionary<string, List<BloqueContenido>>> ObtenerTodosAgrupadosAsync();

    /// <summary>Actualiza el valor de un bloque por clave. Invalida cache.</summary>
    Task<bool> ActualizarAsync(int id, string nuevoValor);

    /// <summary>Invalida el cache (llamado tras edición desde admin).</summary>
    void InvalidarCache();
}

public class ContenidoService : IContenidoService
{
    private const string CacheKeyDiccionario = "contenido:diccionario";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ContenidoService> _logger;

    public ContenidoService(AppDbContext db, IMemoryCache cache, ILogger<ContenidoService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> ObtenerAsync(string clave, string fallback = "")
    {
        var diccionario = await ObtenerDiccionarioAsync();
        return diccionario.TryGetValue(clave, out var valor) ? valor : fallback;
    }

    public async Task<List<BloqueContenido>> ObtenerPorSeccionAsync(string seccion)
    {
        return await _db.BloquesContenido
            .Where(b => b.Seccion == seccion)
            .OrderBy(b => b.Orden)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<BloqueContenido>>> ObtenerTodosAgrupadosAsync()
    {
        var todos = await _db.BloquesContenido
            .OrderBy(b => b.Seccion).ThenBy(b => b.Orden)
            .ToListAsync();

        return todos
            .GroupBy(b => b.Seccion ?? "Sin sección")
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<bool> ActualizarAsync(int id, string nuevoValor)
    {
        var bloque = await _db.BloquesContenido.FindAsync(id);
        if (bloque is null) return false;

        bloque.Valor = nuevoValor ?? string.Empty;
        await _db.SaveChangesAsync();
        InvalidarCache();
        return true;
    }

    public void InvalidarCache() => _cache.Remove(CacheKeyDiccionario);

    private async Task<Dictionary<string, string>> ObtenerDiccionarioAsync()
    {
        if (_cache.TryGetValue<Dictionary<string, string>>(CacheKeyDiccionario, out var cached) && cached is not null)
            return cached;

        var bloques = await _db.BloquesContenido.AsNoTracking().ToListAsync();
        var dict = bloques.ToDictionary(b => b.Clave, b => b.Valor);

        _cache.Set(CacheKeyDiccionario, dict, CacheDuration);
        return dict;
    }
}
