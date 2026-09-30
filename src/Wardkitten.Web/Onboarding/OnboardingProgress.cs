// Feature: F01.04 — asistente de bienvenida (estado del stepper compartido página ↔ layout)
namespace Wardkitten.Web.Onboarding;

/// <summary>
/// Progreso del asistente que comparten la página (<c>Welcome</c>) y su layout (<c>OnboardingLayout</c>, que
/// pinta el stepper): la página avisa del paso actual y el layout pide volver a un paso ya visitado.
/// </summary>
public sealed class OnboardingProgress
{
    public static readonly IReadOnlyList<string> StepTitles = new[] { "Tu perfil", "Cómo avisarte", "Tu primer monitor", "Listo" };

    public int Current { get; private set; }

    /// <summary>Paso más avanzado alcanzado: hasta él se puede volver desde el stepper.</summary>
    public int Furthest { get; private set; }

    public event Action? Changed;
    public event Func<int, Task>? StepRequested;

    public void Set(int step)
    {
        Current = step;
        Furthest = Math.Max(Furthest, step);
        Changed?.Invoke();
    }

    public bool CanGoTo(int step) => step >= 0 && step <= Furthest && step != Current;

    public Task RequestStepAsync(int step)
        => CanGoTo(step) && StepRequested is not null ? StepRequested.Invoke(step) : Task.CompletedTask;
}
