using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Infrastructure.Email;
using PrepDiplomacia.Infrastructure.Identity;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Controllers;

[Route("cuenta")]
public class CuentaController : Controller
{
    private readonly UserManager<UsuarioAplicacion> _userManager;
    private readonly SignInManager<UsuarioAplicacion> _signInManager;
    private readonly IEmailService _email;
    private readonly ILogger<CuentaController> _logger;

    public CuentaController(
        UserManager<UsuarioAplicacion> userManager,
        SignInManager<UsuarioAplicacion> signInManager,
        IEmailService email,
        ILogger<CuentaController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _email = email;
        _logger = logger;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { UrlRetorno = returnUrl });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var resultado = await _signInManager.PasswordSignInAsync(
            vm.Email, vm.Password, vm.RecordarSesion, lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            var usuario = await _userManager.FindByEmailAsync(vm.Email);
            if (usuario is not null)
            {
                usuario.UltimoLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(usuario);

                // Si es admin → al panel admin; si es alumno → al área de alumnos.
                if (await _userManager.IsInRoleAsync(usuario, RolesSistema.Admin))
                    return Redirect(vm.UrlRetorno ?? "/admin");
                if (await _userManager.IsInRoleAsync(usuario, RolesSistema.Alumno))
                    return Redirect(vm.UrlRetorno ?? "/area-alumnos");
            }
            return Redirect(vm.UrlRetorno ?? "/");
        }

        if (resultado.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tu cuenta fue bloqueada temporalmente por demasiados intentos fallidos. Probá de nuevo en 15 minutos.");
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
        return View(vm);
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("registro")]
    [AllowAnonymous]
    public IActionResult Registro() => View(new RegistroAlumnoViewModel());

    [HttpPost("registro")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroAlumnoViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // El registro autoservicio crea cuenta SIN rol Alumno (no tiene acceso al área hasta pagar).
        var usuario = new UsuarioAplicacion
        {
            UserName = vm.Email,
            Email = vm.Email,
            EmailConfirmed = true,
            NombreCompleto = vm.NombreCompleto,
            TieneAccesoArea = false
        };
        var result = await _userManager.CreateAsync(usuario, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }

        await _signInManager.SignInAsync(usuario, isPersistent: false);
        TempData["Mensaje"] = "Tu cuenta fue creada. Ahora podés inscribirte al programa.";
        return Redirect("/inscripcion");
    }

    [HttpGet("recuperar-password")]
    [AllowAnonymous]
    public IActionResult RecuperarPassword() => View(new RecuperarPasswordViewModel());

    [HttpPost("recuperar-password")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarPassword(RecuperarPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await _userManager.FindByEmailAsync(vm.Email);
        // Por seguridad NO revelamos si el email existe o no.
        if (usuario is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var enc = System.Net.WebUtility.UrlEncode(token);
            var url = $"{Request.Scheme}://{Request.Host}/cuenta/reset-password?email={System.Net.WebUtility.UrlEncode(vm.Email)}&token={enc}";

            var html = $$"""
                <p>Recibimos una solicitud para restablecer tu contraseña en Prep Diplomacia.</p>
                <p><a href="{{url}}" style="background:#F3BD2D;color:#061d30;padding:12px 22px;text-decoration:none;font-weight:700;letter-spacing:.08em;text-transform:uppercase;font-size:12px;">Restablecer contraseña</a></p>
                <p>Si no fuiste vos, ignorá este correo.</p>
            """;
            await _email.EnviarAsync(vm.Email, "Restablecer contraseña — Prep Diplomacia", html);
        }

        TempData["Mensaje"] = "Si el correo existe en nuestro sistema, te llegará un mail con instrucciones.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("reset-password")]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return BadRequest();
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await _userManager.FindByEmailAsync(vm.Email);
        if (usuario is null)
        {
            TempData["Mensaje"] = "Listo. Si tu cuenta existe, ya podés iniciar sesión con la nueva contraseña.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ResetPasswordAsync(usuario, vm.Token, vm.Password);
        if (result.Succeeded)
        {
            TempData["Mensaje"] = "Tu contraseña fue actualizada. Ya podés iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(vm);
    }

    [HttpGet("sin-acceso")]
    [AllowAnonymous]
    public IActionResult SinAcceso() => View();

    [HttpGet("cambiar-password")]
    [Authorize]
    public IActionResult CambiarPassword() => View(new CambiarPasswordViewModel());

    [HttpPost("cambiar-password")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(usuario, vm.PasswordActual, vm.PasswordNueva);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(usuario);
            TempData["Mensaje"] = "Contraseña actualizada.";
            return Redirect("/");
        }

        foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(vm);
    }
}
