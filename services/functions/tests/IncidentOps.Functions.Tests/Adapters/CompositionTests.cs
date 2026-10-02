using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Functions.Composition;
using IncidentOps.Functions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentOps.Functions.Tests.Adapters;

public sealed class CompositionTests
{
    [Fact]
    public async Task ResolvesEveryUseCase()
    {
        await using var provider = Build(ServiceProviders.Settings);
        await using var scope = provider.CreateAsyncScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ScheduleAcknowledgementCheck>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CheckAcknowledgementSla>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<PageOnCall>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<MessageSettlement>());
    }

    [Theory]
    [InlineData("ServiceBusConnection")]
    [InlineData("SlaChecksQueue")]
    public void RequiresServiceBusSettings(string key)
    {
        var settings = new Dictionary<string, string?>(ServiceProviders.Settings) { [key] = null };

        Assert.Throws<InvalidOperationException>(() => Build(settings));
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = ServiceProviders.Configuration(settings);
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddIncidentOps(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
