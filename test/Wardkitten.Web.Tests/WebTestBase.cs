using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Wardkitten.Shared.UI.Services;

namespace Wardkitten.Web.Tests;

/// <summary>Contexto bUnit con el cliente de la API real apuntando a <see cref="FakeApi"/>.</summary>
public abstract class WebTestBase : BunitContext
{
    public const string BaseUrl = "http://127.0.0.1:9/";

    protected FakeApi Api { get; } = new();

    // Los agentes de CI son más lentos que un portátil: margen para los tests con polling.
    static WebTestBase() => DefaultWaitTimeout = TimeSpan.FromSeconds(5);

    protected WebTestBase()
    {
        Services.AddSingleton(new WardkittenApiClient(new HttpClient(Api) { BaseAddress = new Uri(BaseUrl) }));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Espera a que se cumpla una condición que no provoca render (p. ej. una llamada de polling):
    /// <c>WaitForAssertion</c> de bUnit solo reevalúa cuando el componente se vuelve a pintar.
    /// </summary>
    protected static async Task Eventually(Func<bool> condition, int timeoutMs = 3000)
    {
        var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > until) throw new TimeoutException("La condición no se cumplió a tiempo.");
            await Task.Delay(10);
        }
    }
}
