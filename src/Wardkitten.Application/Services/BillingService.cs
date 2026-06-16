using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Billing;
using Wardkitten.Application.Common;
using Wardkitten.Domain.Billing;

namespace Wardkitten.Application.Services;

/// <summary>
/// Orquesta la facturación: crea sesiones de checkout (suscripción y recargas) y aplica los cambios que
/// llegan por webhook de Stripe (espejo del estado de la suscripción y crédito de la wallet). Feature: F07.
/// </summary>
public sealed class BillingService
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IUserRepository _users;
    private readonly WalletService _wallet;
    private readonly IPaymentGateway _gateway;

    public BillingService(
        ISubscriptionRepository subscriptions,
        IUserRepository users,
        WalletService wallet,
        IPaymentGateway gateway)
    {
        _subscriptions = subscriptions;
        _users = users;
        _wallet = wallet;
        _gateway = gateway;
    }

    public async Task<Result<string>> CreateSubscriptionCheckoutAsync(string userId, Plan plan, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<string>.Fail("Usuario no encontrado.");
        if (plan == Plan.Free) return Result<string>.Fail("El plan Free no requiere pago.");
        var url = await _gateway.CreateSubscriptionCheckoutAsync(user, plan, successUrl, cancelUrl, ct);
        return Result<string>.Ok(url);
    }

    public async Task<Result<string>> CreateCreditTopUpCheckoutAsync(string userId, decimal credits, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<string>.Fail("Usuario no encontrado.");
        if (credits <= 0) return Result<string>.Fail("La cantidad de créditos debe ser positiva.");
        var url = await _gateway.CreateCreditTopUpCheckoutAsync(user, credits, successUrl, cancelUrl, ct);
        return Result<string>.Ok(url);
    }

    public async Task<Result<string>> CreateBillingPortalAsync(string userId, string returnUrl, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null || user.StripeCustomerId is null)
            return Result<string>.Fail("No hay cliente de facturación asociado.");
        var url = await _gateway.CreateBillingPortalAsync(user, returnUrl, ct);
        return Result<string>.Ok(url);
    }

    /// <summary>Vincula el customer de Stripe a un usuario (desde checkout.session.completed).</summary>
    public async Task SetStripeCustomerAsync(string userId, string stripeCustomerId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null || user.StripeCustomerId == stripeCustomerId) return;
        user.StripeCustomerId = stripeCustomerId;
        await _users.ReplaceAsync(user, ct);
    }

    /// <summary>Aplica el estado de una suscripción recibido por webhook (espejo de Stripe).</summary>
    public async Task ApplySubscriptionUpdateAsync(
        string userId, Plan plan, SubscriptionStatus status,
        string stripeCustomerId, string stripeSubscriptionId, string? stripePriceId,
        DateTime? currentPeriodEndUtc, bool cancelAtPeriodEnd, CancellationToken ct = default)
    {
        var sub = await _subscriptions.GetByStripeSubscriptionIdAsync(stripeSubscriptionId, ct)
                  ?? await _subscriptions.GetByUserAsync(userId, ct);

        var isNew = sub is null;
        sub ??= new Subscription { UserId = userId };
        sub.Plan = plan;
        sub.Status = status;
        sub.StripeCustomerId = stripeCustomerId;
        sub.StripeSubscriptionId = stripeSubscriptionId;
        sub.StripePriceId = stripePriceId;
        sub.CurrentPeriodEndUtc = currentPeriodEndUtc;
        sub.CancelAtPeriodEnd = cancelAtPeriodEnd;

        if (isNew) await _subscriptions.InsertAsync(sub, ct);
        else await _subscriptions.ReplaceAsync(sub, ct);

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            user.Plan = sub.GrantsPaidFeatures ? plan : Plan.Free;
            user.StripeCustomerId = stripeCustomerId;
            await _users.ReplaceAsync(user, ct);
        }
    }

    /// <summary>Acredita una recarga de créditos (idempotente por referencia de pago).</summary>
    public Task<Result> ApplyCreditTopUpAsync(string userId, decimal credits, string paymentReference, CancellationToken ct = default)
        => _wallet.ApplyTopUpAsync(userId, credits, paymentReference, paymentReference, CreditTransactionType.TopUp, ct);
}
