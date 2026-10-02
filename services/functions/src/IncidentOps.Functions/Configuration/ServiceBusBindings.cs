namespace IncidentOps.Functions.Configuration;

public static class ServiceBusBindings
{
    public const string Connection = "ServiceBusConnection";
    public const string IncidentEventsTopic = "%IncidentEventsTopic%";
    public const string SlaSchedulerSubscription = "%SlaSchedulerSubscription%";
    public const string NotifierSubscription = "%NotifierSubscription%";
    public const string SlaChecksQueue = "%SlaChecksQueue%";
}
