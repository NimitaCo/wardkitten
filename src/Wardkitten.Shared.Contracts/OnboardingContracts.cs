// Feature: F01.04 — asistente de bienvenida (onboarding) · F05.05 Telegram · F05.06 prueba de canal
using Wardkitten.Domain.Watches;

namespace Wardkitten.Shared.Contracts;

/// <summary>Estado con el que el asistente retoma al usuario (perfil, canales listos, límites del plan).</summary>
public sealed record OnboardingStateDto(
    bool Completed,
    string Email,
    string DisplayName,
    string TimeZoneId,
    string Locale,
    bool EmailVerified,
    string? Phone,
    bool PhoneVerified,
    bool TelegramLinked,
    bool TelegramAvailable,
    List<ChannelBinding> DefaultChannels,
    long WatchCount,
    string Plan,
    int MaxWatches,
    int MinIntervalSeconds);

public sealed record UpdateProfileRequest(string DisplayName, string TimeZoneId, string Locale);

/// <summary>Canales por defecto (F02.05): los watches nuevos sin bindings propios nacen con ellos.</summary>
public sealed record UpdateChannelsRequest(List<ChannelBinding> Bindings);

public sealed record ChannelTestRequest(ChannelBinding Binding);

public sealed record ChannelTestResultDto(bool Success, string Message);

public sealed record TelegramLinkCodeDto(string Code, string DeepLink, DateTime ExpiresAtUtc);

public sealed record TelegramStatusDto(bool Linked);
