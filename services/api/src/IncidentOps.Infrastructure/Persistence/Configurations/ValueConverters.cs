using IncidentOps.Domain.Alerts;
using IncidentOps.Domain.Catalog;
using IncidentOps.Domain.Incidents;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IncidentOps.Infrastructure.Persistence.Configurations;

internal static class ValueConverters
{
    public static readonly ValueConverter<IncidentId, Guid> IncidentId = new(id => id.Value, value => new IncidentId(value));
    public static readonly ValueConverter<IncidentNumber, int> IncidentNumber = new(number => number.Value, value => new IncidentNumber(value));
    public static readonly ValueConverter<IncidentTitle, string> Title = new(title => title.Value, value => new IncidentTitle(value));
    public static readonly ValueConverter<Description, string> Description = new(description => description.Value, value => new Description(value));
    public static readonly ValueConverter<ServiceId, string> ServiceId = new(id => id.Value, value => new ServiceId(value));
    public static readonly ValueConverter<Actor, string> Actor = new(actor => actor.Value, value => new Actor(value));
    public static readonly ValueConverter<EscalationLevel, int> EscalationLevel = new(level => level.Value, value => new EscalationLevel(value));
    public static readonly ValueConverter<Actor?, string> OptionalActor = new(actor => actor!.Value, value => new Actor(value));
    public static readonly ValueConverter<RootCause?, string> OptionalRootCause = new(cause => cause!.Value, value => new RootCause(value));
    public static readonly ValueConverter<AlertFingerprint?, string> OptionalFingerprint = new(fingerprint => fingerprint!.Value, value => new AlertFingerprint(value));
}
