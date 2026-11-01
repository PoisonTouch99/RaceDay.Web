using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AuthController : BaseController
{
    private readonly ApiService _api;
    public AuthController(ApiService api) => _api = api;

    [HttpGet]
    public IActionResult Login()
    {
        if (IsAuthenticated)
            return CurrentUserRole == "Organiser"
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Home", "Participant");
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var (user, error) = await _api.LoginAsync(model.Email, model.Password);
        if (user is null)
        {
            model.ErrorMessage = error;
            return View(model);
        }
        HttpContext.Session.SignIn(user.UserId, user.FullName, user.Role, user.Email);
        return user.Role == "Organiser"
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Home", "Participant");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var (user, error) = await _api.RegisterAsync(
            model.FullName, model.Email, model.Password, model.Phone, model.Role);
        if (user is null)
        {
            model.ErrorMessage = error;
            return View(model);
        }
        TempData["Success"] = "Account created! Please log in.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        HttpContext.Session.SignOut();
        return RedirectToAction("Login");
    }
}
