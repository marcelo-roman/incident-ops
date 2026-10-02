using Azure.Messaging.ServiceBus;
using IncidentOps.Escalation.Application.Messaging;
using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Functions.Messaging;
using IncidentOps.Functions.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace IncidentOps.Functions.Tests.Host;

public sealed class MessageSettlementTests
{
    private readonly RecordingMessageActions _actions = new();
    private readonly FakeLogger<MessageSettlement> _logger = new();
    private readonly MessageSettlement _settlement;
    private readonly ServiceBusReceivedMessage _message = ServiceBusModelFactory.ServiceBusReceivedMessage(
        body: BinaryData.FromString("{}"),
        messageId: "message-1",
        deliveryCount: 1);

    public MessageSettlementTests()
    {
        _settlement = new MessageSettlement(_logger);
    }

    [Fact]
    public async Task CompletesHandledMessages()
    {
        await _settlement.SettleAsync(_message, _actions, () => Task.FromResult(UseCaseResult.Skipped("nothing to do")), CancellationToken.None);

        Assert.Equal(["complete"], _actions.Settlements);
        Assert.Equal("message-1", _logger.LatestRecord.GetStructuredStateValue("MessageId"));
    }

    [Fact]
    public async Task DeadLettersMalformedMessagesWithoutRetrying()
    {
        await _settlement.SettleAsync(_message, _actions, () => throw new MalformedMessageException("broken"), CancellationToken.None);

        Assert.Equal(["dead-letter"], _actions.Settlements);
        Assert.Equal(MessageSettlement.InvalidMessageReason, _actions.DeadLetterReason);
        Assert.Equal(LogLevel.Error, _logger.LatestRecord.Level);
    }

    [Fact]
    public async Task AbandonsAndRethrowsTransientFailures()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _settlement.SettleAsync(_message, _actions, () => throw new HttpRequestException("api down"), CancellationToken.None));

        Assert.Equal(["abandon"], _actions.Settlements);
        Assert.Equal(LogLevel.Warning, _logger.LatestRecord.Level);
        Assert.IsType<HttpRequestException>(_logger.LatestRecord.Exception);
    }
}
