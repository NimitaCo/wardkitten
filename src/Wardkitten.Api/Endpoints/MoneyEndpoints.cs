using System.Security.Claims;
using Wardkitten.Api.Mapping;
using Wardkitten.Api.Security;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Billing;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Api.Endpoints;

public static class MoneyEndpoints
{
    public static void MapMoneyEndpoints(this IEndpointRouteBuilder app)
    {
        // ---- Wallet de créditos ----
        var wallet = app.MapGroup("/api/wallet").WithTags("Wallet").RequireAuthorization();

        wallet.MapGet("/", async (ClaimsPrincipal p, WalletService svc, CancellationToken ct) =>
        {
            var w = await svc.GetWalletAsync(p.UserId()!, ct);
            return Results.Ok(w.ToDto());
        });

        wallet.MapGet("/transactions", async (ClaimsPrincipal p, WalletService svc, int? skip, int? take, CancellationToken ct) =>
        {
            var txns = await svc.GetTransactionsAsync(p.UserId()!, skip ?? 0, Math.Clamp(take ?? 50, 1, 200), ct);
            return Results.Ok(txns.Select(t => t.ToDto()));
        });

        wallet.MapPost("/topup", async (TopUpRequest req, ClaimsPrincipal p, BillingService billing, IConfiguration config, CancellationToken ct) =>
        {
            var baseUrl = BaseUrl(config);
            var r = await billing.CreateCreditTopUpCheckoutAsync(p.UserId()!, req.Credits,
                $"{baseUrl}/wallet?topup=success", $"{baseUrl}/wallet?topup=cancel", ct);
            return r.Success ? Results.Ok(new CheckoutResponse(r.Value!)) : Results.BadRequest(new { error = r.Error });
        });

        // ---- Suscripción ----
        var billingGroup = app.MapGroup("/api/billing").WithTags("Billing").RequireAuthorization();

        billingGroup.MapPost("/subscribe", async (SubscribeRequest req, ClaimsPrincipal p, BillingService billing, IConfiguration config, CancellationToken ct) =>
        {
            if (!Enum.TryParse<Plan>(req.Plan, ignoreCase: true, out var plan))
                return Results.BadRequest(new { error = "Plan no válido." });
            var baseUrl = BaseUrl(config);
            var r = await billing.CreateSubscriptionCheckoutAsync(p.UserId()!, plan,
                $"{baseUrl}/billing?sub=success", $"{baseUrl}/billing?sub=cancel", ct);
            return r.Success ? Results.Ok(new CheckoutResponse(r.Value!)) : Results.BadRequest(new { error = r.Error });
        });

        billingGroup.MapPost("/portal", async (ClaimsPrincipal p, BillingService billing, IConfiguration config, CancellationToken ct) =>
        {
            var r = await billing.CreateBillingPortalAsync(p.UserId()!, $"{BaseUrl(config)}/billing", ct);
            return r.Success ? Results.Ok(new CheckoutResponse(r.Value!)) : Results.BadRequest(new { error = r.Error });
        });

        // ---- Incidentes ----
        var incidents = app.MapGroup("/api/incidents").WithTags("Incidents").RequireAuthorization();

        incidents.MapGet("/", async (ClaimsPrincipal p, IncidentService svc, int? skip, int? take, CancellationToken ct) =>
        {
            var list = await svc.GetByUserAsync(p.UserId()!, skip ?? 0, Math.Clamp(take ?? 50, 1, 200), ct);
            return Results.Ok(list.Select(i => i.ToDto()));
        });

        incidents.MapPost("/{id}/ack", async (string id, ClaimsPrincipal p, IncidentService svc, CancellationToken ct) =>
        {
            var r = await svc.AcknowledgeAsync(id, p.UserId()!, ct);
            return r.Success ? Results.NoContent() : Results.NotFound(new { error = r.Error });
        });
    }

    private static string BaseUrl(IConfiguration config)
        => (config["PUBLIC_BASE_URL"] ?? "https://www.wardkitten.com").TrimEnd('/');
}
