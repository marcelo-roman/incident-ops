using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IncidentOps.Api.Authentication;
using IncidentOps.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class AuthenticationTests(ApiFactory factory)
{
    private const string TokenPath = "/api/auth/token";

    private readonly HttpClient _anonymous = factory.CreateAnonymousClient();

    [Fact]
    public async Task Valid_credentials_receive_a_signed_bearer_token()
    {
        var before = DateTimeOffset.UtcNow;

        var response = await _anonymous.PostAsJsonAsync(TokenPath, new { username = ApiFactory.DemoUsername, password = ApiFactory.DemoPassword });
        var token = await response.ReadAsync<TokenResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        token.TokenType.Should().Be("Bearer");
        token.ExpiresAt.Should().BeCloseTo(before.AddHours(8), TimeSpan.FromMinutes(1));
        var jwt = new JsonWebToken(token.AccessToken);
        jwt.Alg.Should().Be("HS256");
        jwt.Issuer.Should().Be("incident-ops-api");
        jwt.Audiences.Should().Equal("incident-ops");
        jwt.Subject.Should().Be(ApiFactory.DemoUsername);
        jwt.GetClaim("name").Value.Should().Be(ApiFactory.DemoUsername);
    }

    [Fact]
    public async Task Issued_token_opens_protected_endpoints()
    {
        var token = await (await _anonymous.PostAsJsonAsync(TokenPath, new { username = ApiFactory.DemoUsername, password = ApiFactory.DemoPassword }))
            .ReadAsync<TokenResponse>();

        var response = await SendAsync("/api/services", token.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(ApiFactory.DemoUsername, "wrong-password")]
    [InlineData("someone", ApiFactory.DemoPassword)]
    [InlineData("", "")]
    public async Task Invalid_credentials_return_the_same_401_problem(string username, string password)
    {
        var response = await _anonymous.PostAsJsonAsync(TokenPath, new { username, password });
        var problem = await response.ReadNodeAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        problem["detail"]!.GetValue<string>().Should().Be("Invalid username or password.");
    }

    [Fact]
    public async Task Without_a_configured_password_every_login_is_rejected()
    {
        await using var unconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Auth:DemoPassword", string.Empty));

        var response = await unconfigured.Server.CreateClient().PostAsJsonAsync(TokenPath, new { username = ApiFactory.DemoUsername, password = string.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("shorter-than-32-bytes")]
    public void Startup_fails_without_a_strong_signing_key(string signingKey)
    {
        using var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Auth:SigningKey", signingKey));

        var start = () => misconfigured.Server;

        start.Should().Throw<OptionsValidationException>().WithMessage("*Auth:SigningKey*");
    }

    [Fact]
    public async Task Token_requests_beyond_the_limit_return_429()
    {
        await using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:TokenPermitLimit", "5"));
        var client = limited.Server.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 6; attempt++)
        {
            statuses.Add((await client.PostAsJsonAsync(TokenPath, new { username = ApiFactory.DemoUsername, password = "guess" })).StatusCode);
        }

        statuses.Take(5).Should().OnlyContain(status => status == HttpStatusCode.Unauthorized);
        statuses[5].Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Theory]
    [InlineData("/api/services")]
    [InlineData("/api/incidents")]
    [InlineData("/api/incidents/export")]
    [InlineData("/api/oncall/current")]
    [InlineData("/api/metrics/summary")]
    [InlineData("/metrics")]
    public async Task Protected_endpoints_return_401_without_credentials(string path)
    {
        var response = await _anonymous.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Missing_access_token_challenges_with_bearer()
    {
        var response = await _anonymous.GetAsync("/api/services");

        response.Headers.WwwAuthenticate.Should().ContainSingle().Which.Scheme.Should().Be("Bearer");
    }

    [Theory]
    [InlineData("/api/services")]
    [InlineData("/api/incidents")]
    [InlineData("/api/oncall/current")]
    [InlineData("/api/metrics/summary")]
    public async Task Access_token_opens_console_endpoints(string path)
    {
        var response = await SendAsync(path, TestTokens.Create());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/services")]
    [InlineData("/api/incidents/export")]
    [InlineData("/api/oncall/current")]
    [InlineData("/metrics")]
    public async Task Api_key_opens_service_reads(string path)
    {
        var response = await _anonymous.SendAsync(new HttpRequestMessage(HttpMethod.Get, path).WithApiKey());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metrics_accept_the_api_key_as_bearer_credentials()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/metrics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiFactory.ApiKey);

        var response = await _anonymous.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Access_token_is_rejected_on_service_endpoints()
    {
        var incident = await factory.CreateClient().TriggerAsync();
        var escalate = ApiClientExtensions.Post($"/api/incidents/{incident.Id}/escalate", new { reason = "No ack" });
        escalate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create());

        var escalation = await _anonymous.SendAsync(escalate);
        var metrics = await SendAsync("/metrics", TestTokens.Create());

        escalation.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        metrics.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Api_key_in_the_query_string_is_rejected_outside_alert_ingestion()
    {
        var response = await _anonymous.GetAsync($"/api/services?code={ApiFactory.ApiKey}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wrong_api_key_is_rejected_on_reads()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/services");
        request.Headers.Add("X-Api-Key", "wrong");

        var response = await _anonymous.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public static TheoryData<string> RejectedTokens() => new()
    {
        TestTokens.Create(expires: DateTime.UtcNow.AddMinutes(-5)),
        TestTokens.Create(signingKey: "another-signing-key-with-at-least-32-bytes"),
        TestTokens.Create(issuer: "someone-else"),
        TestTokens.Create(audience: "another-audience"),
        "not-a-token",
    };

    [Theory]
    [MemberData(nameof(RejectedTokens))]
    public async Task Expired_forged_or_foreign_tokens_are_rejected(string token)
    {
        var response = await SendAsync("/api/services", token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/swagger/index.html")]
    public async Task Health_and_swagger_are_anonymous(string path)
    {
        var response = await _anonymous.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_declares_bearer_and_api_key_schemes()
    {
        var document = await (await _anonymous.GetAsync("/swagger/v1/swagger.json")).ReadNodeAsync();
        var schemes = document["components"]!["securitySchemes"]!;

        schemes["Bearer"]!["scheme"]!.GetValue<string>().Should().Be("bearer");
        schemes["ApiKey"]!["name"]!.GetValue<string>().Should().Be("X-Api-Key");
    }

    private Task<HttpResponseMessage> SendAsync(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _anonymous.SendAsync(request);
    }
}
