using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace IncidentOps.Escalation.Infrastructure.Http;

public static class HttpResilience
{
    public static IHttpClientBuilder AddEscalationResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(Configure);
        return builder;
    }

    private static void Configure(HttpStandardResilienceOptions options)
    {
        options.Retry.DisableForUnsafeHttpMethods();
    }
}
