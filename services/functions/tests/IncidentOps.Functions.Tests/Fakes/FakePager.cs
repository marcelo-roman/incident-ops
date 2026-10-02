using IncidentOps.Escalation.Application.Ports;
using IncidentOps.Escalation.Domain.Paging;

namespace IncidentOps.Functions.Tests.Fakes;

internal sealed class FakePager : IPager
{
    public List<PageRequest> Pages { get; } = [];

    public Task PageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        Pages.Add(request);
        return Task.CompletedTask;
    }
}
