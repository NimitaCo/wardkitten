using NSubstitute;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Application;

public class WalletServiceTests
{
    [Fact]
    public async Task Charge_WithFunds_DebitsAndRecordsTransaction()
    {
        var wallets = Substitute.For<IWalletRepository>();
        var txns = Substitute.For<ICreditTransactionRepository>();
        var rates = Substitute.For<IChannelRateRepository>();
        rates.GetCreditsPerMessageAsync(ChannelType.Sms, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(1m);
        wallets.GetOrCreateForUserAsync("u1", Arg.Any<CancellationToken>())
               .Returns(new Wallet { Id = "wal1", UserId = "u1", BalanceCredits = 5m });
        wallets.TryDebitAsync("u1", 1m, Arg.Any<CancellationToken>()).Returns((decimal?)4m);

        var svc = new WalletService(wallets, txns, rates);
        var result = await svc.ChargeForMessageAsync("u1", ChannelType.Sms, "+34600000000");

        result.IsCharged.ShouldBeTrue();
        result.Cost.ShouldBe(1m);
        result.BalanceAfter.ShouldBe(4m);
        await txns.Received(1).InsertAsync(Arg.Any<CreditTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Charge_WithoutFunds_IsInsufficientAndDoesNotRecord()
    {
        var wallets = Substitute.For<IWalletRepository>();
        var txns = Substitute.For<ICreditTransactionRepository>();
        var rates = Substitute.For<IChannelRateRepository>();
        rates.GetCreditsPerMessageAsync(ChannelType.WhatsApp, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(1m);
        wallets.GetOrCreateForUserAsync("u1", Arg.Any<CancellationToken>())
               .Returns(new Wallet { Id = "wal1", UserId = "u1", BalanceCredits = 0m });
        wallets.TryDebitAsync("u1", 1m, Arg.Any<CancellationToken>()).Returns((decimal?)null);

        var svc = new WalletService(wallets, txns, rates);
        var result = await svc.ChargeForMessageAsync("u1", ChannelType.WhatsApp, "+34600000000");

        result.IsInsufficient.ShouldBeTrue();
        await txns.DidNotReceive().InsertAsync(Arg.Any<CreditTransaction>(), Arg.Any<CancellationToken>());
    }
}
