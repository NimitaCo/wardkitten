using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

public enum ChargeOutcome { Free, Charged, InsufficientFunds }

public sealed record ChargeResult(ChargeOutcome Outcome, decimal Cost, decimal? BalanceAfter)
{
    public bool IsCharged => Outcome == ChargeOutcome.Charged;
    public bool IsInsufficient => Outcome == ChargeOutcome.InsufficientFunds;

    public static ChargeResult Free() => new(ChargeOutcome.Free, 0m, null);
    public static ChargeResult Charged(decimal cost, decimal balanceAfter) => new(ChargeOutcome.Charged, cost, balanceAfter);
    public static ChargeResult Insufficient(decimal cost) => new(ChargeOutcome.InsufficientFunds, cost, null);
}

/// <summary>
/// Gestiona la wallet de créditos: cobro de mensajes metered (débito atómico + asiento), recargas
/// (idempotentes por webhook) y reembolsos. Feature: F06.
/// </summary>
public sealed class WalletService
{
    private readonly IWalletRepository _wallets;
    private readonly ICreditTransactionRepository _transactions;
    private readonly IChannelRateRepository _rates;

    public WalletService(IWalletRepository wallets, ICreditTransactionRepository transactions, IChannelRateRepository rates)
    {
        _wallets = wallets;
        _transactions = transactions;
        _rates = rates;
    }

    public async Task<Wallet> GetWalletAsync(string userId, CancellationToken ct = default)
        => await _wallets.GetOrCreateForUserAsync(userId, ct);

    /// <summary>Cobra el coste de un mensaje del canal hacia el destino. No permite saldo negativo.</summary>
    public async Task<ChargeResult> ChargeForMessageAsync(string userId, ChannelType channel, string destination, CancellationToken ct = default)
    {
        var rate = await _rates.GetCreditsPerMessageAsync(channel, destination, ct);
        if (rate <= 0m)
            return ChargeResult.Free();

        var wallet = await _wallets.GetOrCreateForUserAsync(userId, ct);
        var newBalance = await _wallets.TryDebitAsync(userId, rate, ct);
        if (newBalance is null)
            return ChargeResult.Insufficient(rate);

        await _transactions.InsertAsync(new CreditTransaction
        {
            UserId = userId,
            WalletId = wallet.Id,
            Type = CreditTransactionType.Consumption,
            AmountCredits = -rate,
            BalanceAfter = newBalance.Value,
            Channel = channel,
            Reason = $"{channel} → {destination}",
        }, ct);

        return ChargeResult.Charged(rate, newBalance.Value);
    }

    /// <summary>Devuelve créditos (p.ej. si el envío metered falló tras cobrar).</summary>
    public async Task RefundAsync(string userId, decimal amount, string reason, CancellationToken ct = default)
    {
        if (amount <= 0m) return;
        var wallet = await _wallets.GetOrCreateForUserAsync(userId, ct);
        var newBalance = await _wallets.CreditAsync(userId, amount, ct);
        await _transactions.InsertAsync(new CreditTransaction
        {
            UserId = userId,
            WalletId = wallet.Id,
            Type = CreditTransactionType.Refund,
            AmountCredits = amount,
            BalanceAfter = newBalance,
            Reason = reason,
        }, ct);
    }

    /// <summary>Acredita una recarga (desde webhook de Stripe). Idempotente por <paramref name="idempotencyKey"/>.</summary>
    public async Task<Result> ApplyTopUpAsync(string userId, decimal credits, string providerReference, string idempotencyKey,
        CreditTransactionType type = CreditTransactionType.TopUp, CancellationToken ct = default)
    {
        if (credits <= 0m) return Result.Fail("La recarga debe ser positiva.");
        if (await _transactions.ExistsByIdempotencyKeyAsync(idempotencyKey, ct))
            return Result.Ok(); // ya aplicada

        var wallet = await _wallets.GetOrCreateForUserAsync(userId, ct);
        var newBalance = await _wallets.CreditAsync(userId, credits, ct);
        await _transactions.InsertAsync(new CreditTransaction
        {
            UserId = userId,
            WalletId = wallet.Id,
            Type = type,
            AmountCredits = credits,
            BalanceAfter = newBalance,
            ProviderReference = providerReference,
            IdempotencyKey = idempotencyKey,
            Reason = "Recarga de créditos",
        }, ct);
        return Result.Ok();
    }

    public Task<IReadOnlyList<CreditTransaction>> GetTransactionsAsync(string userId, int skip, int take, CancellationToken ct = default)
        => _transactions.GetByUserAsync(userId, skip, take, ct);
}
