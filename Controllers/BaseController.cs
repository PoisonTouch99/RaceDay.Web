using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Controllers;

public class BaseController : Controller
{
    protected bool IsAuthenticated => HttpContext.Session.IsAuthenticated();
    protected string? CurrentUserName => HttpContext.Session.GetUserName();
    protected string? CurrentUserRole => HttpContext.Session.GetUserRole();
    protected int? CurrentUserId => HttpContext.Session.GetUserId();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.IsAuthenticated = IsAuthenticated;
        ViewBag.UserName = CurrentUserName;
        ViewBag.UserRole = CurrentUserRole;
        base.OnActionExecuting(context);
    }

    protected IActionResult RequireAuth()
    {
        if (!IsAuthenticated)
            return RedirectToAction("Login", "Auth");
        return null!;
    }

    protected IActionResult RequireRole(string role)
    {
        var auth = RequireAuth();
        if (auth != null) return auth;
        if (CurrentUserRole != role)
            return RedirectToAction("Index", "Home");
        return null!;
    }
}
