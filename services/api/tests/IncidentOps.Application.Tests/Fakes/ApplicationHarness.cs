using IncidentOps.Application.Catalog;
using IncidentOps.Application.Common;
using IncidentOps.Application.Common.Outbox;
using IncidentOps.Application.Incidents.Messaging;
using IncidentOps.Application.Incidents.ReadModel;
using IncidentOps.Application.Sla;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class ApplicationHarness : IDisposable
{
    private readonly ServiceProvider _provider;

    public ApplicationHarness(double timeScale = 1.0)
    {
        Queries = new InMemoryIncidentQueries(Incidents);
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IClock>(Clock);
        services.AddSingleton<IIncidentRepository>(Incidents);
        services.AddSingleton<IIncidentQueries>(Queries);
        services.AddSingleton<IServiceRepository>(Catalog);
        services.AddSingleton<ICatalogQueries>(Catalog);
        services.AddSingleton<IOnCallRotationRepository>(new FakeOnCallRotationRepository());
        services.AddSingleton<IUnitOfWork>(provider => UnitOfWork ??= new CapturingUnitOfWork(Incidents, provider.GetRequiredService<IDomainEventTranslator>()));
        services.AddSingleton<IEventPublisher>(Events);
        services.AddSingleton<IIncidentNotifier>(Notifier);
        services.AddSingleton(Options.Create(new SlaOptions { TimeScale = timeScale }));
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        UnitOfWork = (CapturingUnitOfWork)_provider.GetRequiredService<IUnitOfWork>();
    }

    public FakeClock Clock { get; } = new();

    public InMemoryIncidentStore Incidents { get; } = new();

    public InMemoryIncidentQueries Queries { get; }

    public FakeServiceRepository Catalog { get; } = new();

    public CapturingUnitOfWork UnitOfWork { get; private set; }

    public RecordingEventPublisher Events { get; } = new();

    public RecordingNotifier Notifier { get; } = new();

    public T Get<T>()
        where T : notnull =>
        _provider.CreateScope().ServiceProvider.GetRequiredService<T>();

    public async Task DispatchOutboxAsync()
    {
        var handler = Get<IOutboxMessageHandler>();
        foreach (var entry in UnitOfWork.Outbox)
        {
            await handler.HandleAsync(entry.Type, entry.Payload, CancellationToken.None);
        }

        UnitOfWork.Outbox.Clear();
    }

    public void Dispose() => _provider.Dispose();
}
