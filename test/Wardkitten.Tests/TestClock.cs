using Wardkitten.Application.Abstractions;

namespace Wardkitten.Tests;

/// <summary>Reloj controlable para tests deterministas.</summary>
public sealed class TestClock : IClock
{
    public TestClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; set; }
}
