using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Wardkitten.Shared.UI.DependencyInjection;
using Wardkitten.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base de la API: configurable por wwwroot/appsettings.json; por defecto, el mismo origen.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl)) apiBaseUrl = builder.HostEnvironment.BaseAddress;

builder.Services.AddWardkittenClient(apiBaseUrl);

await builder.Build().RunAsync();
