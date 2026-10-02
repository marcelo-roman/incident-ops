using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class RecordingMessageActions : ServiceBusMessageActions
{
    public List<string> Settlements { get; } = [];

    public string? DeadLetterReason { get; private set; }

    public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        Settlements.Add("complete");
        return Task.CompletedTask;
    }

    public override Task AbandonMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = default,
        CancellationToken cancellationToken = default)
    {
        Settlements.Add("abandon");
        return Task.CompletedTask;
    }

    public override Task DeadLetterMessageAsync(
        ServiceBusReceivedMessage message,
        Dictionary<string, object>? propertiesToModify = default,
        string? deadLetterReason = default,
        string? deadLetterErrorDescription = default,
        CancellationToken cancellationToken = default)
    {
        Settlements.Add("dead-letter");
        DeadLetterReason = deadLetterReason;
        return Task.CompletedTask;
    }
}
