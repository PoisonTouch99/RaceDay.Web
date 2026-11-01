using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserController : BaseController
{
    private readonly ApiService _api;
    public OrganiserController(ApiService api) => _api = api;

    // ── Dashboard ────────────────────────────────────────────────────────
    public async Task<IActionResult> Dashboard()
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var events = await _api.GetMyEventsAsync();
        return View(events);
    }

    // ── Events ───────────────────────────────────────────────────────────
    public async Task<IActionResult> Events()
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var events = await _api.GetMyEventsAsync();
        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        return View(new EventFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model, IFormFile? bannerImage)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var (ev, error) = await _api.CreateEventAsync(
            model.Name, model.Description, model.EventDate,
            model.Location, model.DistanceKm, model.EventType, model.Status);
        if (ev is null) { model.ErrorMessage = error; return View(model); }

        if (bannerImage is { Length: > 0 })
            await _api.UploadEventBannerAsync(ev.EventId, bannerImage);

        TempData["Success"] = "Event created successfully.";
        return RedirectToAction("Events");
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var ev = await _api.GetEventAsync(id);
        if (ev is null) return NotFound();
        return View(new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            Status = ev.Status
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model, IFormFile? bannerImage)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var (ev, error) = await _api.UpdateEventAsync(
            id, model.Name, model.Description, model.EventDate,
            model.Location, model.DistanceKm, model.EventType, model.Status);
        if (ev is null) { model.ErrorMessage = error; return View(model); }

        if (bannerImage is { Length: > 0 })
            await _api.UploadEventBannerAsync(id, bannerImage);

        TempData["Success"] = "Event updated successfully.";
        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var error = await _api.DeleteEventAsync(id);
        if (error is not null) TempData["Error"] = error;
        else TempData["Success"] = "Event deleted.";
        return RedirectToAction("Events");
    }

    // ── Categories ───────────────────────────────────────────────────────
    public async Task<IActionResult> Categories(int eventId)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var ev = await _api.GetEventAsync(eventId);
        if (ev is null) return NotFound();
        var cats = await _api.GetCategoriesAsync(eventId);
        ViewBag.Event = ev;
        return View(cats);
    }

    [HttpGet]
    public async Task<IActionResult> CreateCategory(int eventId)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var ev = await _api.GetEventAsync(eventId);
        return View(new CategoryFormViewModel { EventId = eventId, EventName = ev?.Name });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var (cat, error) = await _api.CreateCategoryAsync(
            model.EventId, model.Name, model.CategoryType,
            model.DistanceKm, model.MinAge, model.MaxAge,
            model.MaxParticipants, model.EntryFee);
        if (cat is null) { model.ErrorMessage = error; return View(model); }
        TempData["Success"] = "Category added.";
        return RedirectToAction("Categories", new { eventId = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var error = await _api.DeleteCategoryAsync(id);
        if (error is not null) TempData["Error"] = error;
        else TempData["Success"] = "Category deleted.";
        return RedirectToAction("Categories", new { eventId });
    }

    // ── Enrolments ───────────────────────────────────────────────────────
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var ev = await _api.GetEventAsync(eventId);
        if (ev is null) return NotFound();
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        ViewBag.Event = ev;
        return View(enrolments);
    }

    // ── Results ──────────────────────────────────────────────────────────
    public async Task<IActionResult> Results(int eventId)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var ev = await _api.GetEventAsync(eventId);
        if (ev is null) return NotFound();
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        // Load existing results per category
        var allResults = new List<ResultResponse>();
        foreach (var cat in ev.Categories)
            allResults.AddRange(await _api.GetCategoryResultsAsync(cat.CategoryId));

        ViewBag.Event = ev;
        ViewBag.ExistingResults = allResults.ToDictionary(r => r.EnrolmentId);
        return View(enrolments);
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int eventId, int enrolmentId, string? finishTime,
        int? position, string status)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var error = await _api.CreateResultAsync(enrolmentId, finishTime, position, status);
        if (error is not null) TempData["Error"] = error;
        else TempData["Success"] = "Result saved.";
        return RedirectToAction("Results", new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateResult(int eventId, int resultId, string? finishTime,
        int? position, string status)
    {
        var guard = RequireRole("Organiser"); if (guard != null) return guard;
        var error = await _api.UpdateResultAsync(resultId, finishTime, position, status);
        if (error is not null) TempData["Error"] = error;
        else TempData["Success"] = "Result updated.";
        return RedirectToAction("Results", new { eventId });
    }
}
