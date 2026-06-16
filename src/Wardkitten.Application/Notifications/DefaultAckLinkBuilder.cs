using Microsoft.Extensions.Options;

namespace Wardkitten.Application.Notifications;

/// <summary>
/// Implementación por defecto (sin firma) de los magic links. La API la sustituye por una versión
/// firmada con HMAC, expiración y un solo uso (ver SECURITY.md / tech-debt.md).
/// </summary>
public sealed class DefaultAckLinkBuilder : IAckLinkBuilder
{
    private readonly NotificationOptions _options;

    public DefaultAckLinkBuilder(IOptions<NotificationOptions> options) => _options = options.Value;

    public string BuildActionUrl(string incidentId, string watchId, string action)
        => $"{_options.PublicBaseUrl.TrimEnd('/')}/a/{incidentId}?w={watchId}&action={action}";
}
