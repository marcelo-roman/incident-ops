using IncidentOps.Functions.Composition;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.Services.AddIncidentOps(builder.Configuration);
await builder.Build().RunAsync();
