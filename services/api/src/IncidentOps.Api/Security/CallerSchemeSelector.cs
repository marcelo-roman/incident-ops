namespace IncidentOps.Api.Security;

internal static class CallerSchemeSelector
{
    public static string Select(HttpContext context) =>
        ApiKeyReader.HasHeader(context.Request) ? AuthenticationSchemes.ApiKey : AuthenticationSchemes.Bearer;
}
