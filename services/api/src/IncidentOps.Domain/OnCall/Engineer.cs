using IncidentOps.Domain.Common;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.OnCall;

public sealed class Engineer : Entity<string>
{
    public Engineer(string id, string name, EngineerRole role, int rotationOrder)
        : base(Guard.Required(id, nameof(id), 64))
    {
        Name = Guard.Required(name, nameof(name), Actor.MaxLength);
        Role = Guard.Defined(role, nameof(role));
        RotationOrder = rotationOrder;
    }

    private Engineer()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public EngineerRole Role { get; private set; }

    public int RotationOrder { get; private set; }

    public Actor AsActor() => new(Name);
}
