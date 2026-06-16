namespace Wardkitten.Infrastructure.Billing;

/// <summary>Configuración de Stripe (inyectada por secret; ver SECURITY.md). Feature: F07.</summary>
public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Price IDs de las suscripciones mensuales.</summary>
    public string PriceProMonthly { get; set; } = string.Empty;
    public string PriceTeamMonthly { get; set; } = string.Empty;

    /// <summary>Precio (céntimos) de 1 crédito para las recargas (line item dinámico).</summary>
    public long CreditUnitAmountCents { get; set; } = 100;
    public string CreditCurrency { get; set; } = "eur";
}
