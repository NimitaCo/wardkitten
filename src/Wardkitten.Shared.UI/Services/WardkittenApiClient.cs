using System.Net.Http.Json;
using System.Text.Json;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Shared.UI.Services;

/// <summary>Cliente tipado de la API de Wardkitten, compartido por la web y la app móvil.</summary>
public sealed class WardkittenApiClient
{
    private readonly HttpClient _http;

    public WardkittenApiClient(HttpClient http) => _http = http;

    /// <summary>Base de la API (para construir URLs públicas como la de ping).</summary>
    public Uri? BaseAddress => _http.BaseAddress;

    // ---- Auth ----
    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest req) => PostAsync<AuthResponse>("/api/auth/register", req);
    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest req) => PostAsync<AuthResponse>("/api/auth/login", req);
    public Task<ApiResult<UserDto>> GetMeAsync() => GetAsync<UserDto>("/api/auth/me");
    public Task<ApiResult> SendEmailCodeAsync() => PostAsync("/api/auth/email/send-code", new { });
    public Task<ApiResult> VerifyEmailAsync(string code) => PostAsync("/api/auth/email/verify", new VerifyCodeRequest(code));
    public Task<ApiResult> SendPhoneOtpAsync(string phone) => PostAsync("/api/auth/phone/send-otp", new PhoneOtpRequest(phone));
    public Task<ApiResult> VerifyPhoneAsync(string code) => PostAsync("/api/auth/phone/verify", new VerifyCodeRequest(code));
    public Task<ApiResult> RegisterPushTokenAsync(string token) => PostAsync("/api/auth/push-tokens", new PushTokenRequest(token));

    // ---- Watches ----
    public Task<ApiResult<List<WatchDto>>> GetWatchesAsync() => GetAsync<List<WatchDto>>("/api/watches");
    public Task<ApiResult<WatchDto>> GetWatchAsync(string id) => GetAsync<WatchDto>($"/api/watches/{id}");
    public Task<ApiResult<WatchDto>> CreateWatchAsync(WatchRequest req) => PostAsync<WatchDto>("/api/watches", req);
    public Task<ApiResult<WatchDto>> UpdateWatchAsync(string id, WatchRequest req) => PutAsync<WatchDto>($"/api/watches/{id}", req);
    public Task<ApiResult> DeleteWatchAsync(string id) => DeleteAsync($"/api/watches/{id}");
    public Task<ApiResult> CheckInAsync(string id) => PostAsync($"/api/watches/{id}/checkin", new { });
    public Task<ApiResult> PauseAsync(string id) => PostAsync($"/api/watches/{id}/pause", new { });
    public Task<ApiResult> ResumeAsync(string id) => PostAsync($"/api/watches/{id}/resume", new { });
    public Task<ApiResult<List<CheckInDto>>> GetCheckInsAsync(string id) => GetAsync<List<CheckInDto>>($"/api/watches/{id}/checkins");
    public Task<ApiResult<List<WatchTemplateDto>>> GetTemplatesAsync() => GetAsync<List<WatchTemplateDto>>("/api/templates");
    public Task<ApiResult<WatchDto>> CreateFromTemplateAsync(string id) => PostAsync<WatchDto>($"/api/templates/{id}", new { });

    // ---- Wallet & billing ----
    public Task<ApiResult<WalletDto>> GetWalletAsync() => GetAsync<WalletDto>("/api/wallet");
    public Task<ApiResult<List<CreditTransactionDto>>> GetTransactionsAsync() => GetAsync<List<CreditTransactionDto>>("/api/wallet/transactions");
    public Task<ApiResult<CheckoutResponse>> TopUpAsync(decimal credits) => PostAsync<CheckoutResponse>("/api/wallet/topup", new TopUpRequest(credits));
    public Task<ApiResult<CheckoutResponse>> SubscribeAsync(string plan) => PostAsync<CheckoutResponse>("/api/billing/subscribe", new SubscribeRequest(plan));
    public Task<ApiResult<CheckoutResponse>> BillingPortalAsync() => PostAsync<CheckoutResponse>("/api/billing/portal", new { });

    // ---- Status pages ----
    public Task<ApiResult<List<StatusPageDto>>> GetStatusPagesAsync() => GetAsync<List<StatusPageDto>>("/api/status-pages");
    public Task<ApiResult<StatusPageDto>> CreateStatusPageAsync(StatusPageRequest req) => PostAsync<StatusPageDto>("/api/status-pages", req);
    public Task<ApiResult> DeleteStatusPageAsync(string id) => DeleteAsync($"/api/status-pages/{id}");
    public Task<ApiResult<PublicStatusPageDto>> GetPublicStatusAsync(string slug) => GetAsync<PublicStatusPageDto>($"/s/{slug}");

    // ---- Teams & on-call ----
    public Task<ApiResult<List<TeamDto>>> GetTeamsAsync() => GetAsync<List<TeamDto>>("/api/teams");
    public Task<ApiResult<TeamDto>> CreateTeamAsync(string name) => PostAsync<TeamDto>("/api/teams", new CreateTeamRequest(name));
    public Task<ApiResult> DeleteTeamAsync(string id) => DeleteAsync($"/api/teams/{id}");
    public Task<ApiResult> AddTeamMemberAsync(string id, string email) => PostAsync($"/api/teams/{id}/members", new AddMemberRequest(email));
    public Task<ApiResult> RemoveTeamMemberAsync(string id, string userId) => DeleteAsync($"/api/teams/{id}/members/{userId}");
    public Task<ApiResult> SetOnCallAsync(string id, SetOnCallRequest req) => PutAsync($"/api/teams/{id}/oncall", req);
    public Task<ApiResult> AddOnCallOverrideAsync(string id, AddOnCallOverrideRequest req) => PostAsync($"/api/teams/{id}/oncall/overrides", req);

    // ---- Incidents ----
    public Task<ApiResult<List<IncidentDto>>> GetIncidentsAsync() => GetAsync<List<IncidentDto>>("/api/incidents");
    public Task<ApiResult> AckIncidentAsync(string id) => PostAsync($"/api/incidents/{id}/ack", new { });

    // ---- Helpers ----
    private async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        try { return await ReadAsync<T>(await _http.GetAsync(url)); }
        catch (Exception ex) { return ApiResult<T>.Failure(ex.Message); }
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object body)
    {
        try { return await ReadAsync<T>(await _http.PostAsJsonAsync(url, body)); }
        catch (Exception ex) { return ApiResult<T>.Failure(ex.Message); }
    }

    private async Task<ApiResult<T>> PutAsync<T>(string url, object body)
    {
        try { return await ReadAsync<T>(await _http.PutAsJsonAsync(url, body)); }
        catch (Exception ex) { return ApiResult<T>.Failure(ex.Message); }
    }

    private async Task<ApiResult> PostAsync(string url, object body)
    {
        try { return await ReadAsync(await _http.PostAsJsonAsync(url, body)); }
        catch (Exception ex) { return ApiResult.Failure(ex.Message); }
    }

    private async Task<ApiResult> PutAsync(string url, object body)
    {
        try { return await ReadAsync(await _http.PutAsJsonAsync(url, body)); }
        catch (Exception ex) { return ApiResult.Failure(ex.Message); }
    }

    private async Task<ApiResult> DeleteAsync(string url)
    {
        try { return await ReadAsync(await _http.DeleteAsync(url)); }
        catch (Exception ex) { return ApiResult.Failure(ex.Message); }
    }

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode)
        {
            var value = await resp.Content.ReadFromJsonAsync<T>();
            return ApiResult<T>.Success(value!);
        }
        return ApiResult<T>.Failure(await ReadErrorAsync(resp));
    }

    private static async Task<ApiResult> ReadAsync(HttpResponseMessage resp)
        => resp.IsSuccessStatusCode ? ApiResult.Success() : ApiResult.Failure(await ReadErrorAsync(resp));

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp)
    {
        try
        {
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("error", out var error))
                return error.GetString() ?? $"Error {(int)resp.StatusCode}";
        }
        catch { /* ignore */ }
        return $"Error {(int)resp.StatusCode}";
    }
}
