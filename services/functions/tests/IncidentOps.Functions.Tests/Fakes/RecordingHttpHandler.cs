using System.Net;
using System.Text;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class RecordingHttpHandler(HttpStatusCode statusCode, string body = "") : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(await RecordedRequest.FromAsync(request, cancellationToken));
        return new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}

internal sealed record RecordedRequest(HttpMethod Method, Uri? Uri, IReadOnlyDictionary<string, string> Headers, long? ContentLength, string Body)
{
    public static async Task<RecordedRequest> FromAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value));
        var contentLength = request.Content?.Headers.ContentLength;
        return new RecordedRequest(request.Method, request.RequestUri, headers, contentLength, await ReadBodyAsync(request, cancellationToken));
    }

    private static async Task<string> ReadBodyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is null)
        {
            return string.Empty;
        }

        return await request.Content.ReadAsStringAsync(cancellationToken);
    }
}
