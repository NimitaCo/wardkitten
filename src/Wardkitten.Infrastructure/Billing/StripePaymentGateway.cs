using Microsoft.Extensions.Options;
using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;
using Wardkitten.Application.Billing;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Identity;
using SessionService = Stripe.Checkout.SessionService;
using SessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using Plan = Wardkitten.Domain.Billing.Plan;

namespace Wardkitten.Infrastructure.Billing;

/// <summary>Pasarela Stripe: sesiones de checkout para suscripciones y recargas, y portal de cliente.</summary>
public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        if (!string.IsNullOrWhiteSpace(_options.SecretKey))
            StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<string> CreateSubscriptionCheckoutAsync(User user, Plan plan, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var priceId = plan switch
        {
            Plan.Pro => _options.PriceProMonthly,
            Plan.Team => _options.PriceTeamMonthly,
            _ => throw new InvalidOperationException("Plan sin precio configurado."),
        };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            ClientReferenceId = user.Id,
            Customer = user.StripeCustomerId,
            CustomerEmail = user.StripeCustomerId is null ? user.Email : null,
            LineItems = new List<SessionLineItemOptions> { new() { Price = priceId, Quantity = 1 } },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string> { ["userId"] = user.Id, ["plan"] = plan.ToString(), ["kind"] = "subscription" },
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public async Task<string> CreateCreditTopUpCheckoutAsync(User user, decimal credits, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var quantity = (long)Math.Ceiling(credits);
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = user.Id,
            Customer = user.StripeCustomerId,
            CustomerEmail = user.StripeCustomerId is null ? user.Email : null,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _options.CreditCurrency,
                        UnitAmount = _options.CreditUnitAmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "Créditos Wardkitten" },
                    },
                },
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = user.Id,
                ["credits"] = quantity.ToString(),
                ["kind"] = "credit-topup",
            },
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public async Task<string> CreateBillingPortalAsync(User user, string returnUrl, CancellationToken ct = default)
    {
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            ReturnUrl = returnUrl,
        };
        var session = await new Stripe.BillingPortal.SessionService().CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }
}
