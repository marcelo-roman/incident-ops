using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Functions.Configuration;
using IncidentOps.Functions.Messaging;
using Microsoft.Azure.Functions.Worker;

namespace IncidentOps.Functions.Triggers;

public sealed class NotifyOnCallTrigger(PageOnCall useCase, MessageSettlement settlement)
{
    [Function(FunctionNames.NotifyOnCall)]
    public Task RunAsync(
        [ServiceBusTrigger(
            ServiceBusBindings.IncidentEventsTopic,
            ServiceBusBindings.NotifierSubscription,
            Connection = ServiceBusBindings.Connection,
            AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions actions,
        CancellationToken cancellationToken) =>
        settlement.SettleAsync(
            message,
            actions,
            () => useCase.ExecuteAsync(InboundMessages.From(message), cancellationToken),
            cancellationToken);
}
