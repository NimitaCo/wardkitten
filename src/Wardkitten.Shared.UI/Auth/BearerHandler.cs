using System.Net.Http.Headers;

namespace Wardkitten.Shared.UI.Auth;

/// <summary>Adjunta el access token (Bearer) a cada petición a la API.</summary>
public sealed class BearerHandler : DelegatingHandler
{
    private readonly ITokenStore _store;

    public BearerHandler(ITokenStore store) => _store = store;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _store.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
