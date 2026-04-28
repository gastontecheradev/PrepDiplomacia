using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Enums;
using PrepDiplomacia.Infrastructure.Data;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly IBlogService _blog;
    private readonly IMensajeService _mensajes;

    public DashboardController(AppDbContext db, IBlogService blog, IMensajeService mensajes)
    {
        _db = db;
        _blog = blog;
        _mensajes = mensajes;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            TotalPosts            = await _db.Posts.CountAsync(),
            PostsBorrador         = await _db.Posts.CountAsync(p => p.Estado == EstadoPublicacion.Borrador),
            TotalSuscriptores     = await _db.Suscriptores.CountAsync(s => !s.DadoDeBaja),
            TotalInscripciones    = await _db.Inscripciones.CountAsync(),
            InscripcionesActivas  = await _db.Inscripciones.CountAsync(i => i.Estado == EstadoInscripcion.Activa),
            MensajesNoLeidos      = await _mensajes.ContarNoLeidosAsync(),
            ComentariosPendientes = await _db.Comentarios.CountAsync(c => !c.Aprobado),
            UltimosPosts          = await _db.Posts.OrderByDescending(p => p.FechaCreacion).Take(5).ToListAsync(),
            UltimosMensajes       = await _db.Mensajes.OrderByDescending(m => m.FechaCreacion).Take(5).ToListAsync()
        };
        return View(vm);
    }
}
