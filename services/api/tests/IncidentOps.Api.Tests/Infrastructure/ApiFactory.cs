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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:IncidentOps", ConnectionString);
        builder.UseSetting("Database:ApplyMigrations", "true");
        builder.UseSetting("Database:Seed", "true");
        builder.UseSetting("Security:EscalationApiKey", ApiKey);
        builder.UseSetting("RateLimiting:WritePermitLimit", "10000");
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
