// Feature: F02.05 — canales por defecto del usuario (validación de bindings)
using Shouldly;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Application;

public class ChannelBindingRulesTests
{
    private static ChannelBinding B(ChannelType t, string? url = null) => new() { ChannelType = t, DestinationOverride = url };

    [Fact]
    public void Email_IsAlwaysValid_EvenUnverified()
        => ChannelBindingRules.Validate(B(ChannelType.Email), new User { EmailVerified = false }).Success.ShouldBeTrue();

    [Theory]
    [InlineData(ChannelType.Sms, "SMS")]
    [InlineData(ChannelType.WhatsApp, "WhatsApp")]
    public void Metered_RequiresVerifiedPhone(ChannelType type, string name)
    {
        var unverified = ChannelBindingRules.Validate(B(type), new User { Phone = "+34600111222", PhoneVerified = false });
        unverified.Success.ShouldBeFalse();
        unverified.Error.ShouldBe($"Para avisarte por {name} verifica antes tu teléfono.");

        ChannelBindingRules.Validate(B(type), new User { Phone = null, PhoneVerified = true }).Success.ShouldBeFalse();
        ChannelBindingRules.Validate(B(type), new User { Phone = " ", PhoneVerified = true }).Success.ShouldBeFalse();
        ChannelBindingRules.Validate(B(type), new User { Phone = "+34600111222", PhoneVerified = true }).Success.ShouldBeTrue();
    }

    [Fact]
    public void Telegram_RequiresLinkedChat()
    {
        var r = ChannelBindingRules.Validate(B(ChannelType.Telegram), new User());
        r.Success.ShouldBeFalse();
        r.Error.ShouldBe("Vincula antes tu cuenta de Telegram.");
        ChannelBindingRules.Validate(B(ChannelType.Telegram), new User { TelegramChatId = "  " }).Success.ShouldBeFalse();
        ChannelBindingRules.Validate(B(ChannelType.Telegram), new User { TelegramChatId = "12345" }).Success.ShouldBeTrue();
    }

    [Fact]
    public void Push_RequiresARegisteredDevice()
    {
        var r = ChannelBindingRules.Validate(B(ChannelType.Push), new User());
        r.Success.ShouldBeFalse();
        r.Error!.ShouldContain("app móvil");
        ChannelBindingRules.Validate(B(ChannelType.Push), new User { PushTokens = { "fcm" } }).Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ChannelType.Webhook, "Webhook")]
    [InlineData(ChannelType.Slack, "Slack")]
    [InlineData(ChannelType.Discord, "Discord")]
    [InlineData(ChannelType.MicrosoftTeams, "Microsoft Teams")]
    public void Outgoing_RequireHttpsUrl(ChannelType type, string name)
    {
        var missing = ChannelBindingRules.Validate(B(type), new User());
        missing.Success.ShouldBeFalse();
        missing.Error.ShouldBe($"La URL de {name} debe ser una dirección https:// válida.");
        ChannelBindingRules.Validate(B(type, "http://hooks.example.com/x"), new User()).Success.ShouldBeFalse();
        ChannelBindingRules.Validate(B(type, "https://hooks.example.com/x"), new User()).Success.ShouldBeTrue();
    }

    [Fact]
    public void UnknownChannel_IsRejected()
        => ChannelBindingRules.Validate(B((ChannelType)99), new User()).Error.ShouldBe("Canal no soportado.");

    [Theory]
    [InlineData("https://hooks.slack.com/services/T/B/X", true)]
    [InlineData("  https://example.com/hook  ", true)]
    [InlineData("https://8.8.8.8/hook", true)]
    [InlineData("https://[2001:4860:4860::8888]/hook", true)]
    [InlineData("https://172.15.0.1/x", true)]
    [InlineData("https://172.32.0.1/x", true)]
    [InlineData("https://100.63.0.1/x", true)]
    [InlineData("https://100.128.0.1/x", true)]
    [InlineData("https://192.169.0.1/x", true)]
    [InlineData("https://169.253.0.1/x", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("not a url", false)]
    [InlineData("/relative/path", false)]
    [InlineData("http://example.com/hook", false)]
    [InlineData("ftp://example.com/hook", false)]
    [InlineData("https://localhost/hook", false)]
    [InlineData("https://LOCALHOST:8443/hook", false)]
    [InlineData("https://127.0.0.1/hook", false)]
    [InlineData("https://127.8.8.8/hook", false)]
    [InlineData("https://10.1.2.3/hook", false)]
    [InlineData("https://172.16.0.1/hook", false)]
    [InlineData("https://172.31.255.1/hook", false)]
    [InlineData("https://192.168.1.10/hook", false)]
    [InlineData("https://169.254.169.254/latest/meta-data", false)]
    [InlineData("https://100.64.0.1/hook", false)]
    [InlineData("https://100.127.0.1/hook", false)]
    [InlineData("https://0.0.0.0/hook", false)]
    [InlineData("https://[::1]/hook", false)]
    [InlineData("https://[::]/hook", false)]
    [InlineData("https://[fe80::1]/hook", false)]
    [InlineData("https://[fd00::1]/hook", false)]
    [InlineData("https://[fec0::1]/hook", false)]
    [InlineData("https://[::ffff:10.0.0.1]/hook", false)]
    public void IsPublicHttpsUrl(string? url, bool expected)
        => ChannelBindingRules.IsPublicHttpsUrl(url).ShouldBe(expected);

    [Theory]
    [InlineData(ChannelType.Email, "Email")]
    [InlineData(ChannelType.Telegram, "Telegram")]
    [InlineData(ChannelType.Push, "Push")]
    [InlineData(ChannelType.Sms, "SMS")]
    [InlineData(ChannelType.WhatsApp, "WhatsApp")]
    [InlineData(ChannelType.Webhook, "Webhook")]
    [InlineData(ChannelType.Slack, "Slack")]
    [InlineData(ChannelType.Discord, "Discord")]
    [InlineData(ChannelType.MicrosoftTeams, "Microsoft Teams")]
    [InlineData((ChannelType)42, "42")]
    public void DisplayName(ChannelType type, string expected)
        => ChannelDestinations.DisplayName(type).ShouldBe(expected);

    [Fact]
    public void Resolve_PrefersOverride_ThenUserDefaults()
    {
        var user = new User { Email = "a@b.com", TelegramChatId = "77", Phone = "+34600111222", PushTokens = { "t1", "t2" } };
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.Email, DestinationOverride = "x@y.com" }, user).ShouldBe("x@y.com");
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.Email, DestinationOverride = " " }, user).ShouldBe("a@b.com");
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.Telegram }, user).ShouldBe("77");
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.Push }, user).ShouldBe("t1");
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.Sms }, user).ShouldBe("+34600111222");
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.WhatsApp }, user).ShouldBe("+34600111222");
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.Slack }, user).ShouldBeNull();
        ChannelDestinations.Resolve(new ChannelBinding { ChannelType = ChannelType.Push }, new User()).ShouldBeNull();
    }
}
