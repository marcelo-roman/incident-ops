using System.Reflection;
using IncidentOps.Escalation.Application.UseCases;
using IncidentOps.Escalation.Domain.Watches;
using IncidentOps.Escalation.Infrastructure;
using IncidentOps.Functions.Triggers;

namespace IncidentOps.Functions.Tests.Architecture;

internal static class Layers
{
    public const string Domain = "IncidentOps.Escalation.Domain";
    public const string Application = "IncidentOps.Escalation.Application";
    public const string Infrastructure = "IncidentOps.Escalation.Infrastructure";
    public const string Host = "IncidentOps.Functions";

    public static readonly Assembly DomainAssembly = typeof(AcknowledgementWatch).Assembly;
    public static readonly Assembly ApplicationAssembly = typeof(PageOnCall).Assembly;
    public static readonly Assembly InfrastructureAssembly = typeof(InfrastructureRegistration).Assembly;
    public static readonly Assembly HostAssembly = typeof(NotifyOnCallTrigger).Assembly;
}
