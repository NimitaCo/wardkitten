using Shouldly;
using Wardkitten.Domain.Teams;

namespace Wardkitten.Tests.Domain;

public class OnCallScheduleTests
{
    private static readonly DateTime Anchor = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static OnCallSchedule Weekly() => new()
    {
        AnchorUtc = Anchor,
        ShiftSeconds = 24 * 3600, // turnos diarios para el test
        RotationUserIds = new() { "a", "b", "c" },
    };

    [Theory]
    [InlineData(0, "a")]
    [InlineData(1, "b")]
    [InlineData(2, "c")]
    [InlineData(3, "a")]   // vuelve a empezar
    [InlineData(7, "b")]
    public void Rotation_PicksMemberByShift(int daysAfter, string expected)
    {
        Weekly().CurrentOnCall(Anchor.AddDays(daysAfter)).ShouldBe(expected);
    }

    [Fact]
    public void Override_TakesPriorityOverRotation()
    {
        var schedule = Weekly();
        schedule.Overrides.Add(new OnCallOverride
        {
            StartUtc = Anchor.AddDays(1).AddHours(-1),
            EndUtc = Anchor.AddDays(1).AddHours(2),
            UserId = "override-user",
        });

        // Dentro de la ventana del override gana el override; fuera, la rotación normal.
        schedule.CurrentOnCall(Anchor.AddDays(1)).ShouldBe("override-user");
        schedule.CurrentOnCall(Anchor.AddDays(2)).ShouldBe("c");
    }

    [Fact]
    public void EmptyRotation_ReturnsNull()
    {
        new OnCallSchedule { AnchorUtc = Anchor }.CurrentOnCall(Anchor).ShouldBeNull();
    }
}
