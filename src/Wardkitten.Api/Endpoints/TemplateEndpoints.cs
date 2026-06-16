using System.Security.Claims;
using Wardkitten.Api.Mapping;
using Wardkitten.Api.Security;
using Wardkitten.Application.Services;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Api.Endpoints;

public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/templates").WithTags("Templates");

        g.MapGet("/", () => Results.Ok(
            WatchTemplateCatalog.All.Select(t => new WatchTemplateDto(t.Id, t.Name, t.Description, t.Emoji))));

        g.MapPost("/{id}", async (string id, ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
        {
            var input = WatchTemplateCatalog.BuildInput(id);
            if (input is null) return Results.NotFound(new { error = "Plantilla no encontrada." });
            var r = await svc.CreateAsync(p.UserId()!, input, ct);
            return r.Success ? Results.Created($"/api/watches/{r.Value!.Id}", r.Value.ToDto())
                             : Results.BadRequest(new { error = r.Error });
        }).RequireAuthorization();
    }
}
