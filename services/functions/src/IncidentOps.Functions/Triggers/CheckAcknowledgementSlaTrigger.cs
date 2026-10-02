using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Functions.Configuration;
using IncidentOps.Functions.Messaging;
using Microsoft.Azure.Functions.Worker;

namespace IncidentOps.Functions.Triggers;

public sealed class CheckAcknowledgementSlaTrigger(CheckAcknowledgementSla useCase, MessageSettlement settlement)
{
    [Function(FunctionNames.CheckAcknowledgementSla)]
    public Task RunAsync(
        [ServiceBusTrigger(
            ServiceBusBindings.SlaChecksQueue,
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
