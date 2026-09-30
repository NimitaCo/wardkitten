// Feature: F01.04 — asistente de bienvenida · F02.05 canales por defecto · F05.05 Telegram · F05.06 prueba de canal
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EphemeralMongo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Watches;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.IntegrationTests;

/// <summary>
/// El asistente de bienvenida de punta a punta contra la API real (TestServer) y un MongoDB efímero: registro,
/// perfil, canales por defecto, envío de prueba, vinculación de Telegram por el webhook y primer monitor
/// creado con la URL ensayada. Los canales de salida se sustituyen por dobles que registran los envíos.
/// </summary>
public class OnboardingApiTests
{
    private const string WebhookSecret = "it-webhook-secret";

    private sealed class RecordingChannel : INotificationChannel
    {
        public RecordingChannel(ChannelType channel) => Channel = channel;
        public ChannelType Channel { get; }
        public ConcurrentQueue<NotificationMessage> Sent { get; } = new();

        public Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
        {
            Sent.Enqueue(message);
            return Task.FromResult(NotificationResult.Ok("fake"));
        }
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connection;
        public Dictionary<ChannelType, RecordingChannel> Channels { get; } = new[]
        {
            ChannelType.Email, ChannelType.Telegram, ChannelType.Sms, ChannelType.Webhook, ChannelType.Slack,
        }.ToDictionary(t => t, t => new RecordingChannel(t));

        public ApiFactory(string connection) => _connection = connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("MONGOSETTINGS_CONNECTION", _connection);
            builder.UseSetting("MONGOSETTINGS_DATABASENAME", "WardkittenIT_" + Guid.NewGuid().ToString("N")[..8]);
            builder.UseSetting("PUBLIC_BASE_URL", "https://www.wardkitten.test");
            builder.UseSetting("TELEGRAM_BOT_USERNAME", "WardkittenTestBot");
            builder.UseSetting("TELEGRAM_WEBHOOK_SECRET", WebhookSecret);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<INotificationChannel>();
                foreach (var channel in Channels.Values) services.AddSingleton<INotificationChannel>(channel);
            });
        }
    }

    private static async Task<(HttpClient Client, UserDto User)> RegisterAsync(ApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "password123", "Ana", "Europe/Madrid", "es"));
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth.User);
    }

    private static HttpRequestMessage TelegramUpdate(string? secret, long chatId, string text)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/telegram/webhook")
        {
            Content = JsonContent.Create(new
            {
                update_id = 1,
                message = new { message_id = 10, chat = new { id = chatId, type = "private" }, text },
            }),
        };
        if (secret is not null) req.Headers.Add("X-Telegram-Bot-Api-Secret-Token", secret);
        return req;
    }

    [Fact]
    public async Task Wizard_Profile_Channels_Test_FirstPingMonitor_AndComplete()
    {
        using IMongoRunner runner = MongoRunner.Run();
        await using var factory = new ApiFactory(runner.ConnectionString);
        var (api, registered) = await RegisterAsync(factory, "ana@example.com");

        // Recién registrado: el asistente está pendiente.
        registered.OnboardingCompleted.ShouldBeFalse();
        registered.TelegramLinked.ShouldBeFalse();

        var state = (await api.GetFromJsonAsync<OnboardingStateDto>("/api/onboarding/state"))!;
        state.Completed.ShouldBeFalse();
        state.Email.ShouldBe("ana@example.com");
        state.TelegramAvailable.ShouldBeTrue();
        state.DefaultChannels.ShouldBeEmpty();
        state.WatchCount.ShouldBe(0);
        state.Plan.ShouldBe("Free");
        state.MinIntervalSeconds.ShouldBe(3600);

        // 1) Perfil.
        (await api.PutAsJsonAsync("/api/onboarding/profile", new UpdateProfileRequest("Ana García", "Atlantic/Canary", "es")))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        var bad = await api.PutAsJsonAsync("/api/onboarding/profile", new UpdateProfileRequest("Ana", "Nowhere/Land", "es"));
        bad.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // 2) Canales: SMS sin teléfono verificado no se acepta; Email + Slack sí.
        var sms = await api.PutAsJsonAsync("/api/onboarding/channels",
            new UpdateChannelsRequest(new List<ChannelBinding> { new() { ChannelType = ChannelType.Sms } }));
        sms.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var channels = new List<ChannelBinding>
        {
            new() { ChannelType = ChannelType.Email },
            new() { ChannelType = ChannelType.Slack, DestinationOverride = "https://hooks.slack.com/services/T/B/X" },
        };
        (await api.PutAsJsonAsync("/api/onboarding/channels", new UpdateChannelsRequest(channels))).StatusCode.ShouldBe(HttpStatusCode.OK);

        // «Enviar prueba» por Email llega al email de la cuenta; una URL interna se rechaza sin enviar.
        var test = await (await api.PostAsJsonAsync("/api/onboarding/channels/test",
            new ChannelTestRequest(new ChannelBinding { ChannelType = ChannelType.Email }))).Content.ReadFromJsonAsync<ChannelTestResultDto>();
        test!.Success.ShouldBeTrue();
        factory.Channels[ChannelType.Email].Sent.ShouldHaveSingleItem().Destination.ShouldBe("ana@example.com");

        var internalUrl = await (await api.PostAsJsonAsync("/api/onboarding/channels/test",
            new ChannelTestRequest(new ChannelBinding { ChannelType = ChannelType.Webhook, DestinationOverride = "https://10.0.0.5/hook" }))).Content.ReadFromJsonAsync<ChannelTestResultDto>();
        internalUrl!.Success.ShouldBeFalse();
        factory.Channels[ChannelType.Webhook].Sent.ShouldBeEmpty();

        // 3) Primer monitor Ping: se ensaya la URL y el monitor la adopta; nace con los canales por defecto.
        var probe = (await (await api.PostAsJsonAsync("/api/ping-tests", new StartPingTestRequest()))
            .Content.ReadFromJsonAsync<PingTestStateDto>())!;
        (await api.GetAsync($"/p/{probe.Token}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await api.GetFromJsonAsync<PingTestStateDto>($"/api/ping-tests/{probe.ProbeId}"))!.HitCount.ShouldBe(1);

        var create = await api.PostAsJsonAsync("/api/watches", new WatchRequest(
            "Copia de seguridad", null, WatchType.Ping,
            new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 86400, TimeZoneId = "Atlantic/Canary" },
            new Tolerance { GraceSeconds = 1800 }, new List<ChannelBinding>(), Severity.Medium, null, null,
            null, 0, probe.ProbeId));
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var watch = (await create.Content.ReadFromJsonAsync<WatchDto>())!;
        watch.PingToken.ShouldBe(probe.Token);
        watch.ChannelBindings.Select(b => b.ChannelType).ShouldBe(new[] { ChannelType.Email, ChannelType.Slack });

        // Desde ahora las llamadas a esa URL cuentan como check-ins reales.
        (await api.PostAsync($"/p/{probe.Token}", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await api.GetFromJsonAsync<List<CheckInDto>>($"/api/watches/{watch.Id}/checkins"))!.ShouldHaveSingleItem();

        // 4) Listo.
        (await api.PostAsync("/api/onboarding/complete", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var after = (await api.GetFromJsonAsync<OnboardingStateDto>("/api/onboarding/state"))!;
        after.Completed.ShouldBeTrue();
        after.DisplayName.ShouldBe("Ana García");
        after.TimeZoneId.ShouldBe("Atlantic/Canary");
        after.WatchCount.ShouldBe(1);
        after.DefaultChannels.Count.ShouldBe(2);
        (await api.GetFromJsonAsync<UserDto>("/api/auth/me"))!.OnboardingCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task SkipAll_ClosesTheWizard_AndRequiresAuthentication()
    {
        using IMongoRunner runner = MongoRunner.Run();
        await using var factory = new ApiFactory(runner.ConnectionString);

        var anonymous = factory.CreateClient();
        (await anonymous.GetAsync("/api/onboarding/state")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsync("/api/onboarding/skip", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var (api, _) = await RegisterAsync(factory, "salta@example.com");
        (await api.PostAsync("/api/onboarding/skip", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await api.GetFromJsonAsync<UserDto>("/api/auth/me"))!.OnboardingCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Telegram_LinkCode_Webhook_Status_AndUnlink()
    {
        using IMongoRunner runner = MongoRunner.Run();
        await using var factory = new ApiFactory(runner.ConnectionString);
        var (api, _) = await RegisterAsync(factory, "tg@example.com");
        var telegram = factory.Channels[ChannelType.Telegram];

        // Telegram aún no vinculado: no se puede elegir como canal por defecto.
        (await api.PutAsJsonAsync("/api/onboarding/channels",
            new UpdateChannelsRequest(new List<ChannelBinding> { new() { ChannelType = ChannelType.Telegram } })))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var link = (await (await api.PostAsync("/api/auth/telegram/link-code", null)).Content.ReadFromJsonAsync<TelegramLinkCodeDto>())!;
        link.DeepLink.ShouldBe($"https://t.me/WardkittenTestBot?start={link.Code}");

        var web = factory.CreateClient();

        // Sin la cabecera secreta (o con otra) no se procesa nada.
        (await web.SendAsync(TelegramUpdate(null, 555, $"/start {link.Code}"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await web.SendAsync(TelegramUpdate("wrong", 555, $"/start {link.Code}"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await api.GetFromJsonAsync<TelegramStatusDto>("/api/auth/telegram/status"))!.Linked.ShouldBeFalse();

        // Código desconocido: respuesta amable, sin vincular.
        (await web.SendAsync(TelegramUpdate(WebhookSecret, 555, "/start nocode12345"))).StatusCode.ShouldBe(HttpStatusCode.OK);
        telegram.Sent.ShouldHaveSingleItem().Title.ShouldBe("❌ No he podido vincular tu cuenta");
        (await api.GetFromJsonAsync<TelegramStatusDto>("/api/auth/telegram/status"))!.Linked.ShouldBeFalse();

        // Código correcto: se vincula el chat y el bot lo confirma.
        (await web.SendAsync(TelegramUpdate(WebhookSecret, 555, $"/start {link.Code}"))).StatusCode.ShouldBe(HttpStatusCode.OK);
        telegram.Sent.Last().Title.ShouldBe("✅ Cuenta vinculada con Wardkitten");
        telegram.Sent.Last().Destination.ShouldBe("555");
        (await api.GetFromJsonAsync<TelegramStatusDto>("/api/auth/telegram/status"))!.Linked.ShouldBeTrue();
        (await api.GetFromJsonAsync<UserDto>("/api/auth/me"))!.TelegramLinked.ShouldBeTrue();

        // El código es de un solo uso.
        (await web.SendAsync(TelegramUpdate(WebhookSecret, 999, $"/start {link.Code}"))).StatusCode.ShouldBe(HttpStatusCode.OK);
        telegram.Sent.Last().Title.ShouldBe("❌ No he podido vincular tu cuenta");

        // Update irrelevante o mal formado: 200 / 400, sin efectos.
        var noMessage = new HttpRequestMessage(HttpMethod.Post, "/telegram/webhook") { Content = JsonContent.Create(new { update_id = 2 }) };
        noMessage.Headers.Add("X-Telegram-Bot-Api-Secret-Token", WebhookSecret);
        (await web.SendAsync(noMessage)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var garbage = new HttpRequestMessage(HttpMethod.Post, "/telegram/webhook") { Content = new StringContent("{not json") };
        garbage.Headers.Add("X-Telegram-Bot-Api-Secret-Token", WebhookSecret);
        (await web.SendAsync(garbage)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Ya vinculado: Telegram se acepta como canal y la prueba llega al chat.
        (await api.PutAsJsonAsync("/api/onboarding/channels",
            new UpdateChannelsRequest(new List<ChannelBinding> { new() { ChannelType = ChannelType.Telegram } })))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        var test = await (await api.PostAsJsonAsync("/api/onboarding/channels/test",
            new ChannelTestRequest(new ChannelBinding { ChannelType = ChannelType.Telegram }))).Content.ReadFromJsonAsync<ChannelTestResultDto>();
        test!.Success.ShouldBeTrue();
        telegram.Sent.Last().Destination.ShouldBe("555");

        // Desvincular lo quita también de los canales por defecto.
        (await api.PostAsync("/api/auth/telegram/unlink", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await api.GetFromJsonAsync<TelegramStatusDto>("/api/auth/telegram/status"))!.Linked.ShouldBeFalse();
        (await api.GetFromJsonAsync<OnboardingStateDto>("/api/onboarding/state"))!.DefaultChannels.ShouldBeEmpty();
    }
}
