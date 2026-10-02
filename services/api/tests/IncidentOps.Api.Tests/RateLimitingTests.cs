using System.Net;
using System.Net.Http.Json;
using IncidentOps.Api.Tests.Infrastructure;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class RateLimitingTests(ApiFactory factory)
{
    [Fact]
    public async Task Writes_beyond_the_limit_return_429_problem()
    {
        await using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:WritePermitLimit", "2"));
        var client = limited.CreateClient();
        var body = new { title = "Rate limit probe", description = "", serviceId = "search", severity = "Sev4" };

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            statuses.Add((await client.PostAsJsonAsync("/api/incidents", body)).StatusCode);
        }

        statuses.Should().Equal(HttpStatusCode.Created, HttpStatusCode.Created, HttpStatusCode.TooManyRequests);
        (await client.GetAsync("/api/services")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
