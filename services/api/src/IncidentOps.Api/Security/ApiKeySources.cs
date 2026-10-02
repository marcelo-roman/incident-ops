namespace IncidentOps.Api.Security;

[Flags]
internal enum ApiKeySources
{
    Header = 1,
    AuthorizationHeader = 2,
    Query = 4,
}
