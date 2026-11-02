using System.Security.Cryptography;
using System.Text;
using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViewModels;

namespace Controllers;

[AllowAnonymous]
public class AccesoController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccesoController> _logger;

    public AccesoController(
        IConfiguration configuration,
        ILogger<AccesoController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetString(AccessRequiredFilter.SessionKey) == "true")
            return RedirigirLocal(returnUrl);

        ViewData["HideNavigation"] = true;
        ViewData["AccessConfigured"] =
            !string.IsNullOrWhiteSpace(_configuration["AppAccess:Password"]);
        return View(new AccesoViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(AccesoViewModel vm)
    {
        ViewData["HideNavigation"] = true;
        var passwordConfigurada = _configuration["AppAccess:Password"];
        if (string.IsNullOrWhiteSpace(passwordConfigurada))
        {
            _logger.LogError("No está configurada la contraseña de acceso de la aplicación.");
            ModelState.AddModelError(
                string.Empty,
                "Falta configurar la contraseña de acceso. Consultá las instrucciones de configuración.");
            ViewData["AccessConfigured"] = false;
            return View(vm);
        }

        ViewData["AccessConfigured"] = true;
        if (!ModelState.IsValid)
            return View(vm);

        var passwordRecibida = SHA256.HashData(Encoding.UTF8.GetBytes(vm.Password));
        var passwordEsperada = SHA256.HashData(Encoding.UTF8.GetBytes(passwordConfigurada));

        if (!CryptographicOperations.FixedTimeEquals(passwordRecibida, passwordEsperada))
        {
            _logger.LogWarning("Intento de acceso con contraseña incorrecta.");
            ModelState.AddModelError(string.Empty, "La contraseña ingresada es incorrecta.");
            return View(vm);
        }

        HttpContext.Session.SetString(AccessRequiredFilter.SessionKey, "true");
        return RedirigirLocal(vm.ReturnUrl);
    }

    private IActionResult RedirigirLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Pacientes");
}
