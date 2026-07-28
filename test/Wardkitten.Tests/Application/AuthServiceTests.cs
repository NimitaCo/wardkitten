using NSubstitute;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Security;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Identity;

namespace Wardkitten.Tests.Application;

/// <summary>
/// Validación de email en el registro (EmailAddress de Es.Nimita.Domain.Primitives, en vez del
/// antiguo Contains('@')) y del teléfono del OTP (PhoneNumber.TryParseSpanish). La normalización
/// para BUSCAR usuarios existentes (trim + lowercase) se conserva tal cual para no bloquear logins
/// de cuentas ya registradas. Feature: F01.
/// </summary>
public class AuthServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

    private sealed record Harness(
        AuthService Service,
        IUserRepository Users,
        IRefreshTokenRepository RefreshTokens,
        ISubscriptionRepository Subscriptions,
        IWalletRepository Wallets);

    private static Harness Build()
    {
        var users = Substitute.For<IUserRepository>();
        var refreshTokens = Substitute.For<IRefreshTokenRepository>();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var wallets = Substitute.For<IWalletRepository>();
        var passwords = Substitute.For<IPasswordHasher>();
        var tokens = Substitute.For<ITokenService>();

        passwords.Hash(Arg.Any<string>()).Returns(c => "hash:" + c[0]);
        tokens.CreateAccessToken(Arg.Any<User>()).Returns(new AccessToken("jwt", Now.AddMinutes(15)));
        tokens.GenerateRefreshToken().Returns("refresh-token");
        tokens.HashRefreshToken(Arg.Any<string>()).Returns(c => "h:" + c[0]);

        var service = new AuthService(users, refreshTokens, subscriptions, wallets, passwords, tokens, new TestClock(Now));
        return new Harness(service, users, refreshTokens, subscriptions, wallets);
    }

    // ---- Registro: validación de email ----

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@localhost")] // sin punto en el dominio: contiene '@' pero no es un email de negocio
    [InlineData("us er@example.com")]
    [InlineData("")]
    public async Task Register_WithInvalidEmail_Fails(string email)
    {
        var h = Build();

        var result = await h.Service.RegisterAsync(email, "password123", "Alguien", null, null);

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("Email no válido.");
        await h.Users.DidNotReceive().InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WithValidEmail_NormalizesWithTrimAndLowercase()
    {
        var h = Build();
        h.Users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await h.Service.RegisterAsync("  User@Example.COM ", "password123", "Alguien", null, null);

        result.Success.ShouldBeTrue();
        // La MISMA normalización de siempre (trim + lowercase) tanto para buscar como para guardar:
        // cuentas antiguas registradas con emails raros deben seguir encontrándose.
        await h.Users.Received(1).GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>());
        await h.Users.Received(1).InsertAsync(
            Arg.Is<User>(u => u.Email == "user@example.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WithExistingEmail_Fails()
    {
        var h = Build();
        h.Users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(new User { Email = "user@example.com" });

        var result = await h.Service.RegisterAsync("user@example.com", "password123", "Alguien", null, null);

        result.Success.ShouldBeFalse();
        await h.Users.DidNotReceive().InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ---- OTP de teléfono: validación y normalización a E.164 ----

    [Theory]
    [InlineData("600 11 12 22", "+34600111222")] // nacional español -> +34
    [InlineData("+34 600-111-222", "+34600111222")] // internacional con separadores
    [InlineData("0034600111222", "+34600111222")] // prefijo 00
    [InlineData("+447911123456", "+447911123456")] // internacional no español
    public async Task GeneratePhoneOtp_WithValidPhone_NormalizesToE164(string input, string expected)
    {
        var h = Build();
        var user = new User { Id = "u1", Email = "user@example.com" };
        h.Users.GetByIdAsync("u1", Arg.Any<CancellationToken>()).Returns(user);

        var result = await h.Service.GeneratePhoneOtpAsync("u1", input);

        result.Success.ShouldBeTrue();
        user.Phone.ShouldBe(expected);
        user.PhoneVerified.ShouldBeFalse();
        user.PhoneOtpHash.ShouldNotBeNull();
        user.PhoneOtpExpiresUtc.ShouldBe(Now.AddMinutes(30));
        await h.Users.Received(1).ReplaceAsync(user, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")] // demasiado corto incluso como nacional
    [InlineData("512345678")] // 9 dígitos pero no empieza por 6/7/8/9
    [InlineData("")]
    public async Task GeneratePhoneOtp_WithInvalidPhone_FailsAndDoesNotTouchUser(string input)
    {
        var h = Build();
        var user = new User { Id = "u1", Email = "user@example.com", Phone = "+34611111111", PhoneVerified = true };
        h.Users.GetByIdAsync("u1", Arg.Any<CancellationToken>()).Returns(user);

        var result = await h.Service.GeneratePhoneOtpAsync("u1", input);

        result.Success.ShouldBeFalse();
        user.Phone.ShouldBe("+34611111111"); // el teléfono verificado existente no se pisa
        user.PhoneVerified.ShouldBeTrue();
        await h.Users.DidNotReceive().ReplaceAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
