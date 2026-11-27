using System.Net;
using System.Net.Http.Json;

namespace ITSupportSystem.Web.Tests.Infrastructure;

/// <summary>
/// Stands in for ITSupportSystem.API. The Web app's "Api" HttpClient sends its
/// requests here instead of over the network, so tests decide exactly what the
/// API answers and can check exactly what the Web app sent.
/// </summary>
public class FakeApi
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();
    private readonly List<RecordedRequest> _requests = [];
    private readonly Lock _lock = new();
    private bool _unreachable;

    /// <summary>Every request the Web app sent to the API, in order.</summary>
    public IReadOnlyList<RecordedRequest> Requests
    {
        get { lock (_lock) return _requests.ToList(); }
    }

    /// <summary>Answer "METHOD /path" with whatever the responder builds.</summary>
    public void On(HttpMethod method, string path, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        lock (_lock) _routes[Key(method, path)] = responder;
    }

    /// <summary>Answer "METHOD /path" with a status code and a JSON body.</summary>
    public void OnJson(HttpMethod method, string path, HttpStatusCode status, object? body = null) =>
        On(method, path, _ => new HttpResponseMessage(status)
        {
            Content = body is null ? null : JsonContent.Create(body)
        });

    /// <summary>Make every call fail the way it does when the API isn't running.</summary>
    public void GoOffline()
    {
        lock (_lock) _unreachable = true;
    }

    internal async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request)
    {
        // Read the body now: the Web app disposes the request once it has the response.
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
        var path = request.RequestUri!.AbsolutePath;

        Func<HttpRequestMessage, HttpResponseMessage>? responder;
        lock (_lock)
        {
            _requests.Add(new RecordedRequest(
                request.Method,
                path,
                request.Headers.Authorization?.ToString(),
                body));

            if (_unreachable)
            {
                throw new HttpRequestException("No connection could be made (fake API is offline).");
            }

            _routes.TryGetValue(Key(request.Method, path), out responder);
        }

        // Anything a test didn't set up behaves like a route the API doesn't have.
        return responder?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static string Key(HttpMethod method, string path) =>
        $"{method.Method} {path.TrimEnd('/')}".ToUpperInvariant();
}

public record RecordedRequest(HttpMethod Method, string Path, string? Authorization, string? Body);

/// <summary>The HttpMessageHandler that hands every request to the FakeApi.</summary>
internal sealed class FakeApiHandler(FakeApi api) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        api.HandleAsync(request);
}
