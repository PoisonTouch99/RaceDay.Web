using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    // ── Auth ────────────────────────────────────────────────────────────
    public async Task<(UserResponse? user, string? error)> LoginAsync(string email, string password)
    {
        var res = await PostAsync("/api/auth/login", new { email, password });
        if (!res.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(res));
        var user = await ReadAsync<UserResponse>(res);
        // Forward session cookie to MVC session
        ForwardCookies(res);
        return (user, null);
    }

    public async Task<(UserResponse? user, string? error)> RegisterAsync(string fullName, string email,
        string password, string? phone, string role)
    {
        var res = await PostAsync("/api/auth/register", new { fullName, email, password, phone, role });
        if (!res.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(res));
        return (await ReadAsync<UserResponse>(res), null);
    }

    public async Task LogoutAsync() => await PostAsync("/api/auth/logout", null);

    // ── Events ──────────────────────────────────────────────────────────
    public async Task<List<EventResponse>> GetPublishedEventsAsync(string? location = null, string? eventType = null)
    {
        var qs = BuildQuery(("location", location), ("eventType", eventType));
        var res = await GetAsync($"/api/events{qs}");
        return res.IsSuccessStatusCode ? await ReadAsync<List<EventResponse>>(res) ?? new() : new();
    }

    public async Task<EventResponse?> GetEventAsync(int id)
    {
        var res = await GetAsync($"/api/events/{id}");
        return res.IsSuccessStatusCode ? await ReadAsync<EventResponse>(res) : null;
    }

    public async Task<List<EventResponse>> GetMyEventsAsync()
    {
        var res = await GetAsync("/api/events/mine");
        return res.IsSuccessStatusCode ? await ReadAsync<List<EventResponse>>(res) ?? new() : new();
    }

    public async Task<(EventResponse? ev, string? error)> CreateEventAsync(
        string name, string? description, DateTime eventDate, string location,
        decimal distanceKm, string eventType, string status)
    {
        var res = await PostAsync("/api/events", new
        {
            name, description, eventDate, location, distanceKm, eventType, status
        });
        if (!res.IsSuccessStatusCode) return (null, await ReadErrorAsync(res));
        return (await ReadAsync<EventResponse>(res), null);
    }

    public async Task<(EventResponse? ev, string? error)> UpdateEventAsync(int id,
        string name, string? description, DateTime eventDate, string location,
        decimal distanceKm, string eventType, string status)
    {
        var res = await PutAsync($"/api/events/{id}", new
        {
            name, description, eventDate, location, distanceKm, eventType, status
        });
        if (!res.IsSuccessStatusCode) return (null, await ReadErrorAsync(res));
        return (await ReadAsync<EventResponse>(res), null);
    }

    public async Task<string?> DeleteEventAsync(int id)
    {
        var res = await DeleteAsync($"/api/events/{id}");
        return res.IsSuccessStatusCode ? null : await ReadErrorAsync(res);
    }

    public async Task<string?> UploadEventBannerAsync(int eventId, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        var res = await _http.PostAsync($"/api/events/{eventId}/banner", content);
        return res.IsSuccessStatusCode ? null : await ReadErrorAsync(res);
    }

    // ── Categories ──────────────────────────────────────────────────────
    public async Task<List<CategoryResponse>> GetCategoriesAsync(int eventId)
    {
        var res = await GetAsync($"/api/events/{eventId}/categories");
        return res.IsSuccessStatusCode ? await ReadAsync<List<CategoryResponse>>(res) ?? new() : new();
    }

    public async Task<(CategoryResponse? cat, string? error)> CreateCategoryAsync(int eventId,
        string name, string categoryType, decimal? distanceKm, int? minAge, int? maxAge,
        int maxParticipants, decimal entryFee)
    {
        var res = await PostAsync($"/api/events/{eventId}/categories", new
        {
            name, categoryType, distanceKm, minAge, maxAge, maxParticipants, entryFee
        });
        if (!res.IsSuccessStatusCode) return (null, await ReadErrorAsync(res));
        return (await ReadAsync<CategoryResponse>(res), null);
    }

    public async Task<string?> DeleteCategoryAsync(int id)
    {
        var res = await DeleteAsync($"/api/categories/{id}");
        return res.IsSuccessStatusCode ? null : await ReadErrorAsync(res);
    }

    // ── Enrolments ──────────────────────────────────────────────────────
    public async Task<List<EnrolmentResponse>> GetMyEnrolmentsAsync()
    {
        var res = await GetAsync("/api/enrolments/mine");
        return res.IsSuccessStatusCode ? await ReadAsync<List<EnrolmentResponse>>(res) ?? new() : new();
    }

    public async Task<List<EnrolmentResponse>> GetEventEnrolmentsAsync(int eventId)
    {
        var res = await GetAsync($"/api/events/{eventId}/enrolments");
        return res.IsSuccessStatusCode ? await ReadAsync<List<EnrolmentResponse>>(res) ?? new() : new();
    }

    public async Task<(EnrolmentResponse? enrolment, string? error)> EnrolAsync(int categoryId)
    {
        var res = await PostAsync($"/api/categories/{categoryId}/enrolments", null);
        if (!res.IsSuccessStatusCode) return (null, await ReadErrorAsync(res));
        return (await ReadAsync<EnrolmentResponse>(res), null);
    }

    // ── Results ─────────────────────────────────────────────────────────
    public async Task<List<ResultResponse>> GetMyResultsAsync()
    {
        var res = await GetAsync("/api/results/mine");
        return res.IsSuccessStatusCode ? await ReadAsync<List<ResultResponse>>(res) ?? new() : new();
    }

    public async Task<List<ResultResponse>> GetCategoryResultsAsync(int categoryId)
    {
        var res = await GetAsync($"/api/categories/{categoryId}/results");
        return res.IsSuccessStatusCode ? await ReadAsync<List<ResultResponse>>(res) ?? new() : new();
    }

    public async Task<string?> CreateResultAsync(int enrolmentId, string? finishTime, int? position, string status)
    {
        TimeSpan? ft = null;
        if (!string.IsNullOrWhiteSpace(finishTime) && TimeSpan.TryParse(finishTime, out var parsed))
            ft = parsed;
        var res = await PostAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = ft, position, status });
        return res.IsSuccessStatusCode ? null : await ReadErrorAsync(res);
    }

    public async Task<string?> UpdateResultAsync(int resultId, string? finishTime, int? position, string status)
    {
        TimeSpan? ft = null;
        if (!string.IsNullOrWhiteSpace(finishTime) && TimeSpan.TryParse(finishTime, out var parsed))
            ft = parsed;
        var res = await PutAsync($"/api/results/{resultId}", new { finishTime = ft, position, status });
        return res.IsSuccessStatusCode ? null : await ReadErrorAsync(res);
    }

    // ── Users ────────────────────────────────────────────────────────────
    public async Task<UserResponse?> GetProfileAsync()
    {
        var res = await GetAsync("/api/users/me");
        return res.IsSuccessStatusCode ? await ReadAsync<UserResponse>(res) : null;
    }

    public async Task<(UserResponse? user, string? error)> UpdateProfileAsync(string fullName, string? phone)
    {
        var res = await PutAsync("/api/users/me", new { fullName, phone });
        if (!res.IsSuccessStatusCode) return (null, await ReadErrorAsync(res));
        return (await ReadAsync<UserResponse>(res), null);
    }

    public async Task<string?> UploadProfilePictureAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        var res = await _http.PostAsync("/api/users/me/picture", content);
        return res.IsSuccessStatusCode ? null : await ReadErrorAsync(res);
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private HttpRequestMessage BuildRequest(HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        // Forward session cookie from the current MVC request to the API
        var cookie = _ctx.HttpContext?.Request.Cookies[".AspNetCore.Session"];
        if (!string.IsNullOrEmpty(cookie))
            req.Headers.Add("Cookie", $".AspNetCore.Session={cookie}");
        if (body is not null)
        {
            req.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");
        }
        return req;
    }

    private async Task<HttpResponseMessage> GetAsync(string url)
        => await _http.SendAsync(BuildRequest(HttpMethod.Get, url));

    private async Task<HttpResponseMessage> PostAsync(string url, object? body)
        => await _http.SendAsync(BuildRequest(HttpMethod.Post, url, body));

    private async Task<HttpResponseMessage> PutAsync(string url, object? body)
        => await _http.SendAsync(BuildRequest(HttpMethod.Put, url, body));

    private async Task<HttpResponseMessage> DeleteAsync(string url)
        => await _http.SendAsync(BuildRequest(HttpMethod.Delete, url));

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? "An error occurred.";
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                var msgs = new List<string>();
                foreach (var prop in errors.EnumerateObject())
                    foreach (var err in prop.Value.EnumerateArray())
                        msgs.Add(err.GetString() ?? string.Empty);
                return string.Join(" ", msgs);
            }
        }
        catch { }
        return res.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "You must be logged in.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "The requested resource was not found.",
            HttpStatusCode.Conflict => "A conflict occurred. The item may already exist.",
            _ => "An unexpected error occurred. Please try again."
        };
    }

    private void ForwardCookies(HttpResponseMessage res)
    {
        foreach (var header in res.Headers.Where(h => h.Key == "Set-Cookie"))
            foreach (var value in header.Value)
                _ctx.HttpContext?.Response.Headers.Append("Set-Cookie", value);
    }

    private static string BuildQuery(params (string key, string? value)[] pairs)
    {
        var parts = pairs.Where(p => !string.IsNullOrWhiteSpace(p.value))
                         .Select(p => $"{p.key}={Uri.EscapeDataString(p.value!)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? $"?{qs}" : string.Empty;
    }
}
