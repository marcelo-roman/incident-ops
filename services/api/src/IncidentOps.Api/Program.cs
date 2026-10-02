using IncidentOps.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddIncidentOpsApi();

var app = builder.Build();
await app.InitializeDatabaseAsync();
app.UseIncidentOpsPipeline();
app.MapIncidentOpsEndpoints();

await app.RunAsync();

public partial class Program;
