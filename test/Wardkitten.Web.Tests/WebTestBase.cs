using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Wardkitten.Shared.UI.Services;

namespace Wardkitten.Web.Tests;

/// <summary>Contexto bUnit con el cliente de la API real apuntando a <see cref="FakeApi"/>.</summary>
public abstract class WebTestBase : BunitContext
{
    public const string BaseUrl = "http://127.0.0.1:9/";

    protected FakeApi Api { get; } = new();

    protected WebTestBase()
    {
        Services.AddSingleton(new WardkittenApiClient(new HttpClient(Api) { BaseAddress = new Uri(BaseUrl) }));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
