namespace IncidentOps.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string? FullyQualifiedNamespace { get; set; }

    public string? ConnectionString { get; set; }

    public string TopicName { get; set; } = "incident-events";
}
