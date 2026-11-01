using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ParticipantController : BaseController
{
    private readonly ApiService _api;
    public ParticipantController(ApiService api) => _api = api;

    public async Task<IActionResult> Home(string? location, string? eventType)
    {
        var guard = RequireRole("Participant"); if (guard != null) return guard;
        var events = await _api.GetPublishedEventsAsync(location, eventType);
        ViewBag.LocationFilter = location;
        ViewBag.EventTypeFilter = eventType;
        return View(events);
    }

    public async Task<IActionResult> EventDetail(int id)
    {
        var guard = RequireRole("Participant"); if (guard != null) return guard;
        var ev = await _api.GetEventAsync(id);
        if (ev is null) return NotFound();

        // Check if already enrolled
        var myEnrolments = await _api.GetMyEnrolmentsAsync();
        ViewBag.EnrolledCategoryIds = myEnrolments
            .Where(e => e.EventId == id && e.Status != "Cancelled")
            .Select(e => e.CategoryId)
            .ToHashSet();

        return View(ev);
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var guard = RequireRole("Participant"); if (guard != null) return guard;
        var (enrolment, error) = await _api.EnrolAsync(categoryId);
        if (enrolment is null) TempData["Error"] = error;
        else TempData["Success"] = $"You're in! Your bib number is {enrolment.BibNumber}.";
        return RedirectToAction("EventDetail", new { id = eventId });
    }

    public async Task<IActionResult> MyEvents()
    {
        var guard = RequireRole("Participant"); if (guard != null) return guard;
        var enrolments = await _api.GetMyEnrolmentsAsync();
        return View(enrolments);
    }

    public async Task<IActionResult> MyResults()
    {
        var guard = RequireRole("Participant"); if (guard != null) return guard;
        var results = await _api.GetMyResultsAsync();
        // Get total finishers per category for "Xth out of Y" display
        var categoryTotals = new Dictionary<int, int>();
        foreach (var r in results.Where(r => r.Status == "Finished"))
        {
            if (!categoryTotals.ContainsKey(r.CategoryId))
            {
                var all = await _api.GetCategoryResultsAsync(r.CategoryId);
                categoryTotals[r.CategoryId] = all.Count(x => x.Status == "Finished");
            }
        }
        ViewBag.CategoryTotals = categoryTotals;
        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var guard = RequireRole("Participant"); if (guard != null) return guard;
        var user = await _api.GetProfileAsync();
        if (user is null) return RedirectToAction("Login", "Auth");
        return View(new UpdateProfileViewModel
        {
            FullName = user.FullName,
            Phone = user.Phone
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(UpdateProfileViewModel model, IFormFile? profilePicture)
    {
        var guard = RequireRole("Participant"); if (guard != null) return guard;
        var (user, error) = await _api.UpdateProfileAsync(model.FullName, model.Phone);
        if (user is null)
        {
            model.ErrorMessage = error;
            return View(model);
        }
        // Update session name
        HttpContext.Session.SetString("UserName", user.FullName);

        if (profilePicture is { Length: > 0 })
        {
            var picError = await _api.UploadProfilePictureAsync(profilePicture);
            if (picError is not null) { model.ErrorMessage = picError; return View(model); }
        }

        model.SuccessMessage = "Profile updated successfully.";
        model.FullName = user.FullName;
        model.Phone = user.Phone;
        ViewBag.UserName = user.FullName;
        return View(model);
    }
}
