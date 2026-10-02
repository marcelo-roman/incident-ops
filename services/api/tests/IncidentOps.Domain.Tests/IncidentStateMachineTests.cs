using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Tests;

public class IncidentStateMachineTests
{
    public static TheoryData<IncidentStatus, IncidentStatus, bool> Transitions()
    {
        var allowed = new HashSet<(IncidentStatus, IncidentStatus)>
        {
            (IncidentStatus.Triggered, IncidentStatus.Acknowledged),
            (IncidentStatus.Triggered, IncidentStatus.Mitigated),
            (IncidentStatus.Triggered, IncidentStatus.Resolved),
            (IncidentStatus.Acknowledged, IncidentStatus.Mitigated),
            (IncidentStatus.Acknowledged, IncidentStatus.Resolved),
            (IncidentStatus.Mitigated, IncidentStatus.Resolved),
        };

        var data = new TheoryData<IncidentStatus, IncidentStatus, bool>();
        foreach (var from in Enum.GetValues<IncidentStatus>())
        {
            foreach (var to in Enum.GetValues<IncidentStatus>())
            {
                data.Add(from, to, allowed.Contains((from, to)));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Transitions))]
    public void Only_the_contract_transitions_are_allowed(IncidentStatus from, IncidentStatus to, bool expected)
    {
        IncidentStateMachine.CanTransition(from, to).Should().Be(expected);
    }

    [Fact]
    public void Rejected_transition_raises_a_conflict()
    {
        var act = () => IncidentStateMachine.EnsureCanTransition(IncidentStatus.Resolved, IncidentStatus.Triggered);

        act.Should().Throw<InvalidStatusTransitionException>()
            .Which.Should().Match<InvalidStatusTransitionException>(e => e.From == IncidentStatus.Resolved && e.To == IncidentStatus.Triggered);
    }
}
