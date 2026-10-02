using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.UseCases;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IncidentOps.Functions.Messaging;

public sealed partial class MessageSettlement(ILogger<MessageSettlement> logger)
{
    public const string InvalidMessageReason = "InvalidMessage";

    public async Task SettleAsync(
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions actions,
        Func<Task<UseCaseResult>> handle,
        CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(MessageScope.For(message));
        UseCaseResult result;
        try
        {
            result = await handle();
        }
        catch (MalformedMessageException exception)
        {
            await DeadLetterAsync(message, actions, exception, cancellationToken);
            return;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await AbandonAsync(message, actions, exception, cancellationToken);
            throw;
        }

        LogCompleted(message.MessageId, result.Acted, result.Description);
        await actions.CompleteMessageAsync(message, cancellationToken);
    }

    private async Task DeadLetterAsync(
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions actions,
        MalformedMessageException exception,
        CancellationToken cancellationToken)
    {
        LogDeadLettered(exception, message.MessageId);
        await actions.DeadLetterMessageAsync(
            message,
            deadLetterReason: InvalidMessageReason,
            deadLetterErrorDescription: exception.Message,
            cancellationToken: cancellationToken);
    }

    private async Task AbandonAsync(
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions actions,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogAbandoned(exception, message.MessageId, message.DeliveryCount);
        await actions.AbandonMessageAsync(message, cancellationToken: cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Message {MessageId} completed (acted: {Acted}): {Outcome}")]
    private partial void LogCompleted(string messageId, bool acted, string outcome);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message {MessageId} is invalid and was dead-lettered")]
    private partial void LogDeadLettered(Exception exception, string messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message {MessageId} failed on delivery {DeliveryCount} and was abandoned for retry")]
    private partial void LogAbandoned(Exception exception, string messageId, int deliveryCount);
}
