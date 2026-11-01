using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public IActionResult Index()
    {
        if (!IsAuthenticated) return RedirectToAction("Login", "Auth");
        return CurrentUserRole == "Organiser"
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Home", "Participant");
    }
}
