using Microsoft.EntityFrameworkCore;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Data;

namespace PrepDiplomacia.Infrastructure.Services;

public interface IPlanCursoService
{
    Task<List<PlanCurso>> ListarActivosAsync();
    Task<PlanCurso?> ObtenerPorIdAsync(int id);
}

public class PlanCursoService : IPlanCursoService
{
    private readonly AppDbContext _db;
    public PlanCursoService(AppDbContext db) => _db = db;

    public Task<List<PlanCurso>> ListarActivosAsync() =>
        _db.Planes.Where(p => p.Activo).OrderBy(p => p.Orden).ToListAsync();

    public Task<PlanCurso?> ObtenerPorIdAsync(int id) => _db.Planes.FindAsync(id).AsTask();
}
