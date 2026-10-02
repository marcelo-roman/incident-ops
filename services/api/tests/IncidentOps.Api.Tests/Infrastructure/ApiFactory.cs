using System.Net.Http.Headers;
using IncidentOps.Application.Incidents.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace IncidentOps.Api.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ApiKey = "test-api-key";
    public const string SigningKey = "test-signing-key-with-at-least-32-bytes";
    public const string DemoUsername = "demo";
    public const string DemoPassword = "test-demo-password";

    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public RecordingEventPublisher Events { get; } = new();

    public string ConnectionString =>
        new SqlConnectionStringBuilder(_database.GetConnectionString()) { InitialCatalog = "IncidentOps" }.ConnectionString;

    public async Task InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    public HttpClient CreateAnonymousClient() => Server.CreateClient();

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:IncidentOps", ConnectionString);
        builder.UseSetting("Database:ApplyMigrations", "true");
        builder.UseSetting("Database:Seed", "true");
        builder.UseSetting("Security:EscalationApiKey", ApiKey);
        builder.UseSetting("Auth:SigningKey", SigningKey);
        builder.UseSetting("Auth:DemoUsername", DemoUsername);
        builder.UseSetting("Auth:DemoPassword", DemoPassword);
        builder.UseSetting("RateLimiting:WritePermitLimit", "10000");
        builder.UseSetting("RateLimiting:TokenPermitLimit", "10000");
        builder.UseSetting("Chaos:Enabled", "true");
        builder.UseSetting("Observability:GaugeRefreshSeconds", "1");
        builder.UseSetting("Outbox:PollingIntervalMilliseconds", "200");
        builder.UseSetting("Outbox:MaxRetryDelaySeconds", "1");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(Events);
        });
    }
}
