using System.Net;
using IncidentOps.Api.Tests.Infrastructure;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class ChaosTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Injected_faults_fail_api_requests_until_cleared()
    {
        var inject = ApiClientExtensions.Post("/api/chaos/faults", new { errorRate = 1.0, latencyMs = 10, durationSeconds = 60 }).WithApiKey();

        (await _client.SendAsync(inject)).StatusCode.Should().Be(HttpStatusCode.OK);
        var failing = await _client.GetAsync("/api/services");
        var health = await _client.GetAsync("/health/live");
        (await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/chaos/faults").WithApiKey())).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var recovered = await _client.GetAsync("/api/services");

        failing.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        health.StatusCode.Should().Be(HttpStatusCode.OK);
        recovered.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Fault_parameters_are_validated()
    {
        var request = ApiClientExtensions.Post("/api/chaos/faults", new { errorRate = 2.0, latencyMs = 0, durationSeconds = 10 }).WithApiKey();

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
