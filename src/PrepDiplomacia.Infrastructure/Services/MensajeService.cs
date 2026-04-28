using Microsoft.EntityFrameworkCore;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Data;

namespace PrepDiplomacia.Infrastructure.Services;

public interface IMensajeService
{
    Task<MensajeContacto> CrearAsync(MensajeContacto mensaje);
    Task<List<MensajeContacto>> ListarTodosAsync();
    Task<MensajeContacto?> ObtenerPorIdAsync(int id);
    Task<bool> MarcarLeidoAsync(int id);
    Task<bool> EliminarAsync(int id);
    Task<int> ContarNoLeidosAsync();
}

public class MensajeService : IMensajeService
{
    private readonly AppDbContext _db;
    public MensajeService(AppDbContext db) => _db = db;

    public async Task<MensajeContacto> CrearAsync(MensajeContacto mensaje)
    {
        _db.Mensajes.Add(mensaje);
        await _db.SaveChangesAsync();
        return mensaje;
    }

    public Task<List<MensajeContacto>> ListarTodosAsync() =>
        _db.Mensajes.OrderByDescending(m => m.FechaCreacion).ToListAsync();

    public Task<MensajeContacto?> ObtenerPorIdAsync(int id) => _db.Mensajes.FindAsync(id).AsTask();

    public async Task<bool> MarcarLeidoAsync(int id)
    {
        var m = await _db.Mensajes.FindAsync(id);
        if (m is null) return false;
        m.Leido = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var m = await _db.Mensajes.FindAsync(id);
        if (m is null) return false;
        _db.Mensajes.Remove(m);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<int> ContarNoLeidosAsync() => _db.Mensajes.CountAsync(m => !m.Leido);
}
