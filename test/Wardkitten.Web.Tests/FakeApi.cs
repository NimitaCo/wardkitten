using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wardkitten.Web.Tests;

/// <summary>
/// Doble del servidor para <c>WardkittenApiClient</c>: responde por «MÉTODO /ruta» y registra cada llamada
/// con su cuerpo, para afirmar qué envió la UI (el cliente es sellado y habla HTTP de verdad).
/// </summary>
public sealed class FakeApi : HttpMessageHandler
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, Func<string?, HttpResponseMessage>> _routes = new();
    private readonly List<(string Key, string? Body)> _calls = new();

    public IReadOnlyList<(string Key, string? Body)> Calls { get { lock (_calls) return _calls.ToList(); } }

    public FakeApi On(string key, object? body, HttpStatusCode status = HttpStatusCode.OK)
        => On(key, _ => Respond(body, status));

    public FakeApi On(string key, Func<string?, HttpResponseMessage> handler)
    {
        _routes[key] = handler;
        return this;
    }

    public FakeApi Fail(string key, string error, HttpStatusCode status = HttpStatusCode.BadRequest)
        => On(key, new { error }, status);

    public static HttpResponseMessage Respond(object? body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = body is null ? null : JsonContent.Create(body, options: Json) };

    public int Count(string key) => Calls.Count(c => c.Key == key);

    public T? LastBody<T>(string key)
    {
        var body = Calls.Last(c => c.Key == key).Body;
        return body is null ? default : JsonSerializer.Deserialize<T>(body, Json);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
        lock (_calls) _calls.Add((key, body));
        return _routes.TryGetValue(key, out var handler)
            ? handler(body)
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
