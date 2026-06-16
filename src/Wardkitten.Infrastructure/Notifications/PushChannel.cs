using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Infrastructure.Notifications;

public sealed class PushOptions
{
    /// <summary>Contenido del service account JSON de Firebase (inyectado por secret; ver SECURITY.md).</summary>
    public string? ServiceAccountJson { get; set; }
}

/// <summary>Canal Push vía Firebase Cloud Messaging (Android + iOS). Gratuito. Feature: F05.01 / F09.</summary>
public sealed class PushChannel : INotificationChannel
{
    private const string AppName = "wardkitten";
    private static readonly object Gate = new();
    private readonly bool _configured;

    public PushChannel(IOptions<PushOptions> options)
    {
        var json = options.Value.ServiceAccountJson;
        if (string.IsNullOrWhiteSpace(json)) return;

        lock (Gate)
        {
            if (GetAppOrNull() is null)
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromJson(json) }, AppName);
        }
        _configured = true;
    }

    public ChannelType Channel => ChannelType.Push;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        var app = GetAppOrNull();
        if (!_configured || app is null)
            return NotificationResult.Fail("FCM no configurado.");

        try
        {
            var data = new Dictionary<string, string>
            {
                ["watchId"] = message.WatchId ?? string.Empty,
                ["incidentId"] = message.IncidentId ?? string.Empty,
            };
            if (!string.IsNullOrEmpty(message.AckUrl)) data["ackUrl"] = message.AckUrl!;

            var fcm = new Message
            {
                Token = message.Destination,
                Notification = new Notification { Title = message.Title, Body = message.Body },
                Data = data,
            };
            var id = await FirebaseMessaging.GetMessaging(app).SendAsync(fcm, ct);
            return NotificationResult.Ok(id);
        }
        catch (Exception ex)
        {
            return NotificationResult.Fail(ex.Message);
        }
    }

    private static FirebaseApp? GetAppOrNull()
    {
        try { return FirebaseApp.GetInstance(AppName); }
        catch (Exception) { return null; } // GetInstance lanza si la app no existe todavía
    }
}
