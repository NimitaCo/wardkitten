using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Identity;

namespace Wardkitten.Application.Billing;

/// <summary>
/// Pasarela de pago (Stripe en infraestructura). Crea sesiones de checkout para suscripciones y para
/// recargas de créditos, y el portal de cliente. El procesamiento de webhooks vive en la API e invoca a
/// <c>BillingService</c>/<c>WalletService</c>. Feature: F07.
/// </summary>
public interface IPaymentGateway
{
    Task<string> CreateSubscriptionCheckoutAsync(User user, Plan plan, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task<string> CreateCreditTopUpCheckoutAsync(User user, decimal credits, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task<string> CreateBillingPortalAsync(User user, string returnUrl, CancellationToken ct = default);
}
