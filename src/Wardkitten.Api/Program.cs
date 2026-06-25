using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Wardkitten.Api.Endpoints;
using Wardkitten.Api.Mcp;
using Wardkitten.Api.RealTime;
using Wardkitten.Application.DependencyInjection;
using Wardkitten.Application.RealTime;
using Wardkitten.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---- Capas ----
builder.Services.AddWardkittenApplication(config["PUBLIC_BASE_URL"]);
builder.Services.AddWardkittenInfrastructure(config);
builder.Services.AddWardkittenIntegrations(config);

// Tiempo real (sustituye al publicador no-op de Application).
builder.Services.AddSignalR();
builder.Services.AddSingleton<IWatchEventPublisher, SignalRWatchEventPublisher>();

// ---- Autenticación JWT ----
var jwtSecret = config["JWT_SECRET"] ?? "wardkitten-dev-insecure-secret-change-me-please-0123456789";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["JWT_ISSUER"] ?? "wardkitten",
            ValidateAudience = true,
            ValidAudience = config["JWT_AUDIENCE"] ?? "wardkitten",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            NameClaimType = "sub",
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
        // Permite el token por query string para el handshake de SignalR.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// ---- CORS (app móvil; la web va same-origin al estar hospedada por la API) ----
var corsOrigins = (config["CORS_ORIGINS"] ?? "http://localhost:5173,https://www.wardkitten.com")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---- Rate limiting por IP (ver SECURITY.md §3) ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", http => PerIp(http, permitLimit: 10));
    options.AddPolicy("ping", http => PerIp(http, permitLimit: 120));
});

builder.Services.AddOpenApi();

// MCP (Model Context Protocol): interfaz/protocolo —NO IA— para que un agente externo opere Wardkitten
// en el futuro. Las herramientas viven en WardkittenMcpTools y resuelven el usuario del JWT.
builder.Services.AddHttpContextAccessor();
builder.Services.AddMcpServer().WithHttpTransport().WithTools<WardkittenMcpTools>();

var app = builder.Build();

// Crea índices y la colección time-series (idempotente). No tumbar el arranque si Mongo no responde aún.
try
{
    await app.Services.InitializeWardkittenInfrastructureAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "No se pudo inicializar MongoDB en el arranque; se reintentará en el worker.");
}

// Documentación de la API expuesta SIEMPRE (también en producción) para los usuarios:
//   - OpenAPI JSON: /openapi/v1.json
//   - Swagger UI:   /swagger
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Wardkitten API");
    o.DocumentTitle = "Wardkitten · API";
});

// La web (Blazor WASM) la sirve la propia API (un solo despliegue): assets estáticos + framework WASM.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "wardkitten-api" })).WithTags("Health");

app.MapAuthEndpoints();
app.MapWatchEndpoints();
app.MapTemplateEndpoints();
app.MapStatusPageEndpoints();
app.MapTeamEndpoints();
app.MapMoneyEndpoints();
app.MapPublicEndpoints();
app.MapInternalEndpoints();
app.MapHub<WatchHub>("/hubs/watch");

// Endpoint MCP (Streamable HTTP), protegido por JWT (ver SECURITY.md). Feature: F14.
app.MapMcp("/mcp").RequireAuthorization();

// Fallback SPA: toda ruta no resuelta por la API ni por un fichero estático sirve el index.html
// del WASM, para que el enrutado del lado cliente (Blazor) funcione con recargas y deep-links.
app.MapFallbackToFile("index.html");

app.Run();

static RateLimitPartition<string> PerIp(HttpContext http, int permitLimit)
    => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = permitLimit });

public partial class Program;
