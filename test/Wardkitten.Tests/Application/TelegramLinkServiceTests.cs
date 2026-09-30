// Feature: F05.05 — vinculación de Telegram por deep link
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.Security;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Application;

public class TelegramLinkServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);

    private sealed class Harness
    {
        public required TelegramLinkService Service { get; init; }
        public required IUserRepository Users { get; init; }
        public required INotificationChannel Telegram { get; init; }
        public required TestClock Clock { get; init; }
        public required User User { get; init; }
        public List<NotificationMessage> Sent { get; } = new();
    }

    private static Harness Build(string botUsername = "WardkittenBot", string secret = "s3cret", bool withChannel = true)
    {
        var user = new User { Id = "u1", Email = "ana@example.com" };
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync("u1", Arg.Any<CancellationToken>()).Returns(user);
        var tokens = Substitute.For<ITokenService>();
        tokens.HashRefreshToken(Arg.Any<string>()).Returns(c => "h:" + c[0]);
        var telegram = Substitute.For<INotificationChannel>();
        telegram.Channel.Returns(ChannelType.Telegram);
        var email = Substitute.For<INotificationChannel>();
        email.Channel.Returns(ChannelType.Email);
        var clock = new TestClock(Now);
        var options = Options.Create(new TelegramLinkOptions { BotUsername = botUsername, WebhookSecret = secret });
        var channels = withChannel ? new[] { email, telegram } : new[] { email };
        var h = new Harness
        {
            Service = new TelegramLinkService(users, tokens, channels, options, clock),
            Users = users, Telegram = telegram, Clock = clock, User = user,
        };
        telegram.SendAsync(Arg.Do<NotificationMessage>(m => h.Sent.Add(m)), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Ok());
        return h;
    }

    private static void PendingCode(Harness h, string code, DateTime expires)
    {
        h.User.TelegramLinkCodeHash = "h:" + code;
        h.User.TelegramLinkExpiresUtc = expires;
        h.Users.GetByTelegramLinkCodeHashAsync("h:" + code, Arg.Any<CancellationToken>()).Returns(h.User);
    }

    // ---- Código y deep link ----

    [Fact]
    public async Task CreateLinkCode_StoresOnlyTheHash_WithFifteenMinutesOfLife()
    {
        var h = Build("@WardkittenBot ");

        var r = await h.Service.CreateLinkCodeAsync("u1");

        r.Success.ShouldBeTrue();
        var code = r.Value!.Code;
        code.Length.ShouldBe(12);
        code.ShouldMatch("^[a-z2-9]+$");
        r.Value.DeepLink.ShouldBe($"https://t.me/WardkittenBot?start={code}");
        r.Value.ExpiresAtUtc.ShouldBe(Now.AddMinutes(15));
        h.User.TelegramLinkCodeHash.ShouldBe("h:" + code);
        h.User.TelegramLinkExpiresUtc.ShouldBe(Now.AddMinutes(15));
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLinkCode_GeneratesADifferentCodeEachTime()
    {
        var h = Build();
        var a = (await h.Service.CreateLinkCodeAsync("u1")).Value!.Code;
        var b = (await h.Service.CreateLinkCodeAsync("u1")).Value!.Code;
        a.ShouldNotBe(b);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateLinkCode_FailsWhenTheBotIsNotConfigured(string bot)
    {
        var h = Build(bot);
        h.Service.IsConfigured.ShouldBeFalse();
        (await h.Service.CreateLinkCodeAsync("u1")).Error.ShouldBe("Telegram no está disponible en este momento.");
        await h.Users.DidNotReceive().ReplaceAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLinkCode_FailsForUnknownUser()
    {
        var h = Build();
        h.Service.IsConfigured.ShouldBeTrue();
        (await h.Service.CreateLinkCodeAsync("nope")).Error.ShouldBe("Usuario no encontrado.");
    }

    // ---- Estado y desvinculación ----

    [Fact]
    public async Task IsLinked_ReflectsTheChat()
    {
        var h = Build();
        (await h.Service.IsLinkedAsync("u1")).ShouldBeFalse();
        (await h.Service.IsLinkedAsync("nope")).ShouldBeFalse();
        h.User.TelegramChatId = "42";
        (await h.Service.IsLinkedAsync("u1")).ShouldBeTrue();
    }

    [Fact]
    public async Task Unlink_ClearsTheChat_AndRemovesTelegramFromDefaults()
    {
        var h = Build();
        h.User.TelegramChatId = "42";
        h.User.TelegramLinkCodeHash = "h:x";
        h.User.TelegramLinkExpiresUtc = Now;
        h.User.DefaultChannelBindings = new List<ChannelBinding>
        {
            new() { ChannelType = ChannelType.Email }, new() { ChannelType = ChannelType.Telegram },
        };

        (await h.Service.UnlinkAsync("u1")).Success.ShouldBeTrue();

        h.User.TelegramChatId.ShouldBeNull();
        h.User.TelegramLinkCodeHash.ShouldBeNull();
        h.User.TelegramLinkExpiresUtc.ShouldBeNull();
        h.User.DefaultChannelBindings.ShouldHaveSingleItem().ChannelType.ShouldBe(ChannelType.Email);
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
        (await h.Service.UnlinkAsync("nope")).Error.ShouldBe("Usuario no encontrado.");
    }

    // ---- Secreto del webhook ----

    [Theory]
    [InlineData("s3cret", "s3cret", true)]
    [InlineData("s3cret", "S3CRET", false)]
    [InlineData("s3cret", "s3cre", false)]
    [InlineData("s3cret", "", false)]
    [InlineData("s3cret", null, false)]
    [InlineData("", "", false)]
    [InlineData("", "anything", false)]
    public void WebhookSecret_MustMatchExactly_AndBeConfigured(string configured, string? header, bool expected)
        => Build(secret: configured).Service.IsValidWebhookSecret(header).ShouldBe(expected);

    // ---- /start ----

    [Fact]
    public async Task Start_WithAValidCode_LinksTheChat_AndConfirms()
    {
        var h = Build();
        PendingCode(h, "abcdefghjkmn", Now.AddMinutes(10));

        var outcome = await h.Service.HandleMessageAsync(987654321, "  /start ABCDEFGHJKMN  ");

        outcome.ShouldBe(TelegramUpdateOutcome.Linked);
        h.User.TelegramChatId.ShouldBe("987654321");
        h.User.TelegramLinkCodeHash.ShouldBeNull();
        h.User.TelegramLinkExpiresUtc.ShouldBeNull();
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
        var reply = h.Sent.ShouldHaveSingleItem();
        reply.Destination.ShouldBe("987654321");
        reply.Channel.ShouldBe(ChannelType.Telegram);
        reply.Title.ShouldBe("✅ Cuenta vinculada con Wardkitten");
        reply.Body.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Start_AddressedToTheBot_InAGroup_AlsoWorks()
    {
        var h = Build();
        PendingCode(h, "abcdefghjkmn", Now.AddMinutes(10));

        (await h.Service.HandleMessageAsync(-100200, "/start@WardkittenBot abcdefghjkmn")).ShouldBe(TelegramUpdateOutcome.Linked);
        h.User.TelegramChatId.ShouldBe("-100200");
    }

    [Fact]
    public async Task Start_ExactlyAtExpiry_StillLinks()
    {
        var h = Build();
        PendingCode(h, "abcdefghjkmn", Now);
        (await h.Service.HandleMessageAsync(1, "/start abcdefghjkmn")).ShouldBe(TelegramUpdateOutcome.Linked);
    }

    [Fact]
    public async Task Start_WithAnExpiredCode_DoesNotLink()
    {
        var h = Build();
        PendingCode(h, "abcdefghjkmn", Now.AddSeconds(-1));

        (await h.Service.HandleMessageAsync(1, "/start abcdefghjkmn")).ShouldBe(TelegramUpdateOutcome.InvalidCode);

        h.User.TelegramChatId.ShouldBeNull();
        await h.Users.DidNotReceive().ReplaceAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        h.Sent.ShouldHaveSingleItem().Title.ShouldBe("❌ No he podido vincular tu cuenta");
        h.Sent[0].Destination.ShouldBe("1");
    }

    [Fact]
    public async Task Start_WithACodeWithoutExpiry_DoesNotLink()
    {
        var h = Build();
        h.Users.GetByTelegramLinkCodeHashAsync("h:abc", Arg.Any<CancellationToken>()).Returns(h.User);
        (await h.Service.HandleMessageAsync(1, "/start abc")).ShouldBe(TelegramUpdateOutcome.InvalidCode);
    }

    [Fact]
    public async Task Start_WithAnUnknownCode_RepliesWithAFriendlyError()
    {
        var h = Build();
        (await h.Service.HandleMessageAsync(5, "/start zzzz")).ShouldBe(TelegramUpdateOutcome.InvalidCode);
        h.Sent.ShouldHaveSingleItem().Body.ShouldContain("caducado");
    }

    [Theory]
    [InlineData("/start")]
    [InlineData("  /START  ")]
    [InlineData("/start@WardkittenBot")]
    public async Task Start_WithoutCode_RepliesWithInstructions(string text)
    {
        var h = Build();
        (await h.Service.HandleMessageAsync(5, text)).ShouldBe(TelegramUpdateOutcome.MissingCode);
        var reply = h.Sent.ShouldHaveSingleItem();
        reply.Destination.ShouldBe("5");
        reply.Title.ShouldBe("👋 Hola, soy Wardkitten");
        reply.Body.ShouldContain("Vincular");
        await h.Users.DidNotReceive().GetByTelegramLinkCodeHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hola")]
    [InlineData("/help")]
    [InlineData("/starts abc")]
    [InlineData("start abc")]
    public async Task OtherMessages_AreIgnored_Silently(string? text)
    {
        var h = Build();
        (await h.Service.HandleMessageAsync(5, text)).ShouldBe(TelegramUpdateOutcome.Ignored);
        h.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Linking_Survives_AFailingReply()
    {
        var h = Build();
        PendingCode(h, "abc", Now.AddMinutes(1));
        h.Telegram.SendAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("down"));

        (await h.Service.HandleMessageAsync(7, "/start abc")).ShouldBe(TelegramUpdateOutcome.Linked);
        h.User.TelegramChatId.ShouldBe("7");
    }

    [Fact]
    public async Task Linking_WorksEvenWithoutTheTelegramChannelRegistered()
    {
        var h = Build(withChannel: false);
        PendingCode(h, "abc", Now.AddMinutes(1));
        (await h.Service.HandleMessageAsync(7, "/start abc")).ShouldBe(TelegramUpdateOutcome.Linked);
        h.Sent.ShouldBeEmpty();
    }
}
