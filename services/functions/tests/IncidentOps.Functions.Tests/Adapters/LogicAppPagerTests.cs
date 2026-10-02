using System.Net;
using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Paging;
using IncidentOps.Functions.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Functions.Tests.Adapters;

public sealed class LogicAppPagerTests
{
    [Fact]
    public async Task PostsTheContractPayloadToTheLogicApp()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.Accepted);
        using var provider = ServiceProviders.WithHttpHandler(handler);
        var page = Assert.IsType<PagingDecision.Page>(PagingDecision.Decide(Sample.Window(2), Sample.Rotation()));

        await provider.GetRequiredService<IPager>().PageAsync(page.Request, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("https://logic.example.com/workflows/notify/invoke?sig=secret"), request.Uri);
        Assert.Equal(
            $$"""{"incidentNumber":1042,"title":"Checkout returns 502","severity":"Sev1","serviceId":"checkout","escalationLevel":2,"target":"bruno.costa","url":"https://incidents.example.com/incidents/{{Sample.IncidentGuid}}"}""",
            request.Body);
    }

    [Fact]
    public async Task SendsABufferedBodyBecauseLogicAppTriggersRejectChunkedRequests()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.Accepted);
        using var provider = ServiceProviders.WithHttpHandler(handler);
        var page = Assert.IsType<PagingDecision.Page>(PagingDecision.Decide(Sample.Window(2), Sample.Rotation()));

        await provider.GetRequiredService<IPager>().PageAsync(page.Request, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(request.Body), request.ContentLength);
    }

    [Fact]
    public async Task FailsWhenTheLogicAppRejects()
    {
        using var provider = ServiceProviders.WithHttpHandler(new RecordingHttpHandler(HttpStatusCode.BadRequest));
        var page = Assert.IsType<PagingDecision.Page>(PagingDecision.Decide(Sample.Window(), Sample.Rotation()));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetRequiredService<IPager>().PageAsync(page.Request, CancellationToken.None));
    }
}
