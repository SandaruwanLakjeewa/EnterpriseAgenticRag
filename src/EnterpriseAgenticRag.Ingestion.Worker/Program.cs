using EnterpriseAgenticRag.Infrastructure;
using EnterpriseAgenticRag.Infrastructure.Persistence;
using EnterpriseAgenticRag.Ingestion.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddEnterpriseRagInfrastructure(builder.Configuration);
builder.Services.AddHostedService<IngestionWorker>();

var host = builder.Build();
if (host.Services.GetRequiredService<IConfiguration>().GetValue("Database:InitializeOnStartup", true))
    await host.Services.InitializeDevelopmentDatabaseAsync();
host.Run();
