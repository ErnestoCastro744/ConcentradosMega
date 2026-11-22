using System.ComponentModel.DataAnnotations;
using ConcentradosMega.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ConcentradosMega.Controllers;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese el correo"), EmailAddress(ErrorMessage = "Correo no válido")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la contraseña"), DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Clave { get; set; } = "";

    [Display(Name = "Recordarme")]
    public bool Recordarme { get; set; }
}

[Authorize]
public class AccountController : Controller
{
    private readonly SignInManager<Usuario> _signIn;
    private readonly UserManager<Usuario> _users;

    public AccountController(SignInManager<Usuario> signIn, UserManager<Usuario> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var usuario = await _users.FindByEmailAsync(model.Correo);
        if (usuario == null || !usuario.Activo)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(model);
        }

        var r = await _signIn.PasswordSignInAsync(usuario, model.Clave, model.Recordarme, lockoutOnFailure: true);
        if (r.Succeeded)
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Home");

        ModelState.AddModelError("", r.IsLockedOut
            ? "Cuenta bloqueada temporalmente por intentos fallidos."
            : "Correo o contraseña incorrectos.");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}
