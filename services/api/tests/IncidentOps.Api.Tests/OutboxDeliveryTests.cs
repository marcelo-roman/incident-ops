using IncidentOps.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;

namespace IncidentOps.Api.Tests;

[Collection(ApiFixtureGroup.Name)]
public class OutboxDeliveryTests(ApiFactory factory)
{
    [Fact]
    public async Task Events_are_stored_with_the_change_and_retried_until_published()
    {
        await WaitUntilOutboxIsDrainedAsync();
        factory.Events.FailuresLeft = 2;

        var incident = await factory.CreateClient().TriggerAsync(title: "Outbox retry probe");
        var types = await factory.Events.WaitForTypesAsync(incident.Id, 1);
        var (attempts, processed, deadLettered) = await OutboxRowAsync(incident.Id);

        types.Should().Equal("incident.triggered");
        attempts.Should().Be(3);
        processed.Should().BeTrue();
        deadLettered.Should().BeFalse();
    }

    private async Task WaitUntilOutboxIsDrainedAsync()
    {
        for (var attempt = 0; attempt < 100 && await PendingAsync() > 0; attempt++)
        {
            await Task.Delay(100);
        }
    }

    private async Task<int> PendingAsync()
    {
        await using var connection = new SqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM OutboxMessages WHERE ProcessedAt IS NULL AND DeadLetteredAt IS NULL", connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private async Task<(int Attempts, bool Processed, bool DeadLettered)> OutboxRowAsync(Guid incidentId)
    {
        await using var connection = new SqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT Attempts, ProcessedAt, DeadLetteredAt FROM OutboxMessages WHERE Payload LIKE @incident AND Payload LIKE '%incident.triggered%'",
            connection);
        command.Parameters.AddWithValue("@incident", $"%{incidentId}%");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt32(0), !reader.IsDBNull(1), !reader.IsDBNull(2));
    }
}
