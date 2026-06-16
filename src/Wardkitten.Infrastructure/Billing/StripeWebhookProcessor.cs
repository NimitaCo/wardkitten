using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Wardkitten.Application.Abstractions.Persistence;
using BillingService = Wardkitten.Application.Services.BillingService;
using Plan = Wardkitten.Domain.Billing.Plan;
using SubscriptionStatus = Wardkitten.Domain.Billing.SubscriptionStatus;

namespace Wardkitten.Infrastructure.Billing;

/// <summary>
/// Procesa los webhooks de Stripe con <b>verificación de firma</b> (ver SECURITY.md §3) y traduce los
/// eventos a cambios de suscripción/wallet vía <see cref="BillingService"/>. Idempotente por referencia.
/// </summary>
public sealed class StripeWebhookProcessor
{
    private readonly StripeOptions _options;
    private readonly BillingService _billing;
    private readonly IUserRepository _users;
    private readonly ILogger<StripeWebhookProcessor> _logger;

    public StripeWebhookProcessor(IOptions<StripeOptions> options, BillingService billing, IUserRepository users, ILogger<StripeWebhookProcessor> logger)
    {
        _options = options.Value;
        _billing = billing;
        _users = users;
        _logger = logger;
    }

    /// <summary>Devuelve false si la firma no es válida (la API debe responder 400).</summary>
    public async Task<bool> ProcessAsync(string json, string signatureHeader, CancellationToken ct = default)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _options.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Firma de webhook de Stripe inválida");
            return false;
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                if (stripeEvent.Data.Object is Session session)
                    await HandleCheckoutCompletedAsync(session, ct);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                if (stripeEvent.Data.Object is Subscription subscription)
                    await HandleSubscriptionAsync(subscription, stripeEvent.Type == "customer.subscription.deleted", ct);
                break;
        }
        return true;
    }

    private async Task HandleCheckoutCompletedAsync(Session session, CancellationToken ct)
    {
        var userId = session.ClientReferenceId;
        if (string.IsNullOrEmpty(userId)) return;

        if (!string.IsNullOrEmpty(session.CustomerId))
            await _billing.SetStripeCustomerAsync(userId, session.CustomerId, ct);

        if (session.Mode == "payment"
            && session.Metadata is not null
            && session.Metadata.TryGetValue("kind", out var kind) && kind == "credit-topup"
            && session.Metadata.TryGetValue("credits", out var creditsRaw)
            && decimal.TryParse(creditsRaw, out var credits))
        {
            var reference = session.PaymentIntentId ?? session.Id;
            await _billing.ApplyCreditTopUpAsync(userId, credits, reference, ct);
        }
    }

    private async Task HandleSubscriptionAsync(Subscription subscription, bool deleted, CancellationToken ct)
    {
        var user = await _users.GetByStripeCustomerIdAsync(subscription.CustomerId, ct);
        if (user is null) return;

        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        var plan = deleted ? Plan.Free : MapPlan(priceId);
        var status = deleted ? SubscriptionStatus.Canceled : MapStatus(subscription.Status);

        await _billing.ApplySubscriptionUpdateAsync(
            user.Id, plan, status,
            subscription.CustomerId, subscription.Id, priceId,
            currentPeriodEndUtc: null, // se omite por compatibilidad entre versiones de la API
            cancelAtPeriodEnd: subscription.CancelAtPeriodEnd,
            ct);
    }

    private Plan MapPlan(string? priceId)
    {
        if (!string.IsNullOrEmpty(priceId) && priceId == _options.PriceTeamMonthly) return Plan.Team;
        if (!string.IsNullOrEmpty(priceId) && priceId == _options.PriceProMonthly) return Plan.Pro;
        return Plan.Free;
    }

    private static SubscriptionStatus MapStatus(string status) => status switch
    {
        "active" => SubscriptionStatus.Active,
        "trialing" => SubscriptionStatus.Trialing,
        "past_due" or "unpaid" => SubscriptionStatus.PastDue,
        "canceled" or "incomplete_expired" => SubscriptionStatus.Canceled,
        "incomplete" => SubscriptionStatus.Incomplete,
        _ => SubscriptionStatus.Incomplete,
    };
}
