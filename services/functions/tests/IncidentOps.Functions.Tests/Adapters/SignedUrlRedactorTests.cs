using IncidentOps.Functions.Telemetry;
using Microsoft.ApplicationInsights.DataContracts;

namespace IncidentOps.Functions.Tests.Adapters;

public sealed class SignedUrlRedactorTests
{
    [Fact]
    public void StripsSignedQueryFromDependencyData()
    {
        var dependency = new DependencyTelemetry { Data = "https://logic.example.com/workflows/notify/invoke?api-version=2016-10-01&sig=secret" };

        new SignedUrlRedactor().Initialize(dependency);

        Assert.Equal("https://logic.example.com/workflows/notify/invoke", dependency.Data);
    }

    [Fact]
    public void KeepsUnsignedDependencyData()
    {
        var dependency = new DependencyTelemetry { Data = "https://incidents-api.example.com/api/incidents?open=true" };

        new SignedUrlRedactor().Initialize(dependency);

        Assert.Equal("https://incidents-api.example.com/api/incidents?open=true", dependency.Data);
    }
}
