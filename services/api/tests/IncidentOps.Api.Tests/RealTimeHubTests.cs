using System.Net;
using System.Text.Json;
using IncidentOps.Api.Tests.Infrastructure;
using IncidentOps.Application.Common;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class RealTimeHubTests(ApiFactory factory)
{
    [Fact]
    public async Task Hub_broadcasts_incident_changes_and_timeline_entries()
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/incidents"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(TestTokens.Create());
            })
            .AddJsonProtocol(options => ContractJson.Configure(options.PayloadSerializerOptions))
            .Build();

        var changed = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var appended = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>("IncidentChanged", incident => changed.TrySetResult(incident));
        connection.On<JsonElement>("TimelineAppended", entry => appended.TrySetResult(entry));
        await connection.StartAsync();

        var incident = await factory.CreateClient().TriggerAsync(title: "Hub broadcast check");

        var changedPayload = await changed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var appendedPayload = await appended.Task.WaitAsync(TimeSpan.FromSeconds(10));
        changedPayload.GetProperty("id").GetGuid().Should().Be(incident.Id);
        changedPayload.GetProperty("status").GetString().Should().Be("Triggered");
        changedPayload.GetProperty("acknowledgementBreached").GetBoolean().Should().BeFalse();
        appendedPayload.GetProperty("incidentId").GetGuid().Should().Be(incident.Id);
        appendedPayload.GetProperty("kind").GetString().Should().Be("Triggered");
    }

    [Fact]
    public async Task Negotiate_requires_an_access_token()
    {
        var response = await factory.CreateAnonymousClient().PostAsync("/hubs/incidents/negotiate?negotiateVersion=1", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Negotiate_accepts_the_access_token_from_the_query_string()
    {
        var response = await factory.CreateAnonymousClient()
            .PostAsync($"/hubs/incidents/negotiate?negotiateVersion=1&access_token={TestTokens.Create()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
