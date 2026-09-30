using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Wardkitten.Shared.UI.DependencyInjection;

namespace Wardkitten.Web;

// Main explícito (sin instrucciones de nivel superior): Stryker.NET compila el proyecto como biblioteca para
// mutarlo y las instrucciones de nivel superior no compilan así (CS8805). Ver AGENTS.md → mutation testing.
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Base de la API: configurable por wwwroot/appsettings.json; por defecto, el mismo origen.
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl)) apiBaseUrl = builder.HostEnvironment.BaseAddress;

        builder.Services.AddWardkittenClient(apiBaseUrl);

        await builder.Build().RunAsync();
    }
}
