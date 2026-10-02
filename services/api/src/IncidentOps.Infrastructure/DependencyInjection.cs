using IncidentOps.Application.Catalog;
using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents.ReadModel;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;
using IncidentOps.Infrastructure.Messaging;
using IncidentOps.Infrastructure.Outbox;
using IncidentOps.Infrastructure.Persistence;
using IncidentOps.Infrastructure.Persistence.ReadModel;
using IncidentOps.Infrastructure.Persistence.Repositories;
using IncidentOps.Infrastructure.Seeding;
using IncidentOps.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{DatabaseOptions.ConnectionStringName}' is required.");
        }

        services.AddDbContext<IncidentOpsDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddDbContext<IncidentOpsReadDbContext>(options => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IOnCallRotationRepository, OnCallRotationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIncidentQueries, IncidentQueries>();
        services.AddScoped<ICatalogQueries, CatalogQueries>();
        services.AddScoped<DatabaseInitializer>();
        services.AddSingleton<OutboxSignal>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddHostedService(provider => provider.GetRequiredService<OutboxDispatcher>());

        var serviceBus = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>() ?? new ServiceBusOptions();
        return services.AddEventPublishing(serviceBus);
    }
}
