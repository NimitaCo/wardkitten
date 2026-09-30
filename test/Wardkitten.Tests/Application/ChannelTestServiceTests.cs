// Feature: F05.06 — envío de prueba por un canal
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Application;

public class ChannelTestServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);

    private sealed record Harness(
        ChannelTestService Service,
        User User,
        Dictionary<ChannelType, INotificationChannel> Channels,
        List<NotificationMessage> Sent,
        List<NotificationLog> Logged);

    private static Harness Build(params ChannelType[] available)
    {
        var user = new User { Id = "u1", Email = "ana@example.com", TelegramChatId = "42", Phone = "+34600111222", PhoneVerified = true };
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync("u1", Arg.Any<CancellationToken>()).Returns(user);
        var sent = new List<NotificationMessage>();
        var logged = new List<NotificationLog>();
        var logs = Substitute.For<INotificationLogRepository>();
        logs.InsertAsync(Arg.Do<NotificationLog>(logged.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var channels = new Dictionary<ChannelType, INotificationChannel>();
        foreach (var type in available)
        {
            var c = Substitute.For<INotificationChannel>();
            c.Channel.Returns(type);
            c.SendAsync(Arg.Do<NotificationMessage>(sent.Add), Arg.Any<CancellationToken>()).Returns(NotificationResult.Ok("pm-1"));
            channels[type] = c;
        }
        var service = new ChannelTestService(channels.Values, users, logs, new TestClock(Now));
        return new Harness(service, user, channels, sent, logged);
    }

    [Fact]
    public async Task Email_GoesToTheAccountEmail_AndIsLogged()
    {
        var h = Build(ChannelType.Email, ChannelType.Telegram);

        var r = await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = ChannelType.Email });

        r.Success.ShouldBeTrue();
        r.Value.ShouldBe("Prueba enviada por Email. Comprueba que te ha llegado.");
        var msg = h.Sent.ShouldHaveSingleItem();
        msg.Channel.ShouldBe(ChannelType.Email);
        msg.Destination.ShouldBe("ana@example.com");
        msg.Title.ShouldBe("🐾 Wardkitten · mensaje de prueba");
        msg.Body.ShouldContain("por Email");
        msg.Severity.ShouldBe(Severity.Low);

        var log = h.Logged.ShouldHaveSingleItem();
        log.UserId.ShouldBe("u1");
        log.Kind.ShouldBe("test");
        log.Channel.ShouldBe(ChannelType.Email);
        log.Destination.ShouldBe("ana@example.com");
        log.Success.ShouldBeTrue();
        log.ProviderMessageId.ShouldBe("pm-1");
        log.SentAtUtc.ShouldBe(Now);
    }

    [Fact]
    public async Task Telegram_GoesToTheLinkedChat()
    {
        var h = Build(ChannelType.Telegram);
        (await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = ChannelType.Telegram })).Success.ShouldBeTrue();
        h.Sent.ShouldHaveSingleItem().Destination.ShouldBe("42");
    }

    [Fact]
    public async Task Webhook_GoesToTheGivenUrl_Trimmed()
    {
        var h = Build(ChannelType.Slack);
        var r = await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = ChannelType.Slack, DestinationOverride = " https://hooks.slack.com/x " });
        r.Value.ShouldBe("Prueba enviada por Slack. Comprueba que te ha llegado.");
        h.Sent.ShouldHaveSingleItem().Destination.ShouldBe("https://hooks.slack.com/x");
    }

    [Theory]
    [InlineData(ChannelType.Sms, "SMS")]
    [InlineData(ChannelType.WhatsApp, "WhatsApp")]
    public async Task Metered_AreNeverTested(ChannelType type, string name)
    {
        var h = Build(type);
        var r = await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = type });
        r.Error.ShouldBe($"Las pruebas por {name} no se envían porque consumen créditos.");
        h.Sent.ShouldBeEmpty();
        h.Logged.ShouldBeEmpty();
    }

    [Fact]
    public async Task InvalidBinding_IsRejectedBeforeSending()
    {
        var h = Build(ChannelType.Webhook);
        var r = await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = ChannelType.Webhook, DestinationOverride = "https://10.0.0.1/x" });
        r.Error.ShouldBe("La URL de Webhook debe ser una dirección https:// válida.");
        h.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnavailableChannel_Fails()
    {
        var h = Build(ChannelType.Email);
        var r = await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = ChannelType.Discord, DestinationOverride = "https://discord.com/api/webhooks/1" });
        r.Error.ShouldBe("El canal Discord no está disponible ahora mismo.");
    }

    [Fact]
    public async Task UnknownUser_Fails()
    {
        var h = Build(ChannelType.Email);
        (await h.Service.SendTestAsync("nope", new ChannelBinding { ChannelType = ChannelType.Email })).Error.ShouldBe("Usuario no encontrado.");
    }

    [Fact]
    public async Task ProviderFailure_IsReported_AndLogged()
    {
        var h = Build(ChannelType.Email);
        h.Channels[ChannelType.Email].SendAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Fail("SMTP no configurado"));

        var r = await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = ChannelType.Email });

        r.Success.ShouldBeFalse();
        r.Error.ShouldBe("No se pudo enviar la prueba por Email: SMTP no configurado");
        var log = h.Logged.ShouldHaveSingleItem();
        log.Success.ShouldBeFalse();
        log.Error.ShouldBe("SMTP no configurado");
    }

    [Fact]
    public async Task ProviderException_IsReportedAsAFailure()
    {
        var h = Build(ChannelType.Email);
        h.Channels[ChannelType.Email].SendAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var r = await h.Service.SendTestAsync("u1", new ChannelBinding { ChannelType = ChannelType.Email });

        r.Error.ShouldBe("No se pudo enviar la prueba por Email: boom");
        h.Logged.ShouldHaveSingleItem().Error.ShouldBe("boom");
    }
}
