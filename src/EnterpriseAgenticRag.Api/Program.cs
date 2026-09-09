using EnterpriseAgenticRag.Api;
using EnterpriseAgenticRag.Application;
using EnterpriseAgenticRag.Infrastructure;
using EnterpriseAgenticRag.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestIdentity, HttpCurrentRequestIdentity>();
builder.Services.AddEnterpriseRagInfrastructure(builder.Configuration);

string authenticationMode = builder.Configuration["Authentication:Mode"] ?? "Development";
if (authenticationMode.Equals("Development", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddAuthentication("Development")
        .AddScheme<DevelopmentAuthenticationOptions, DevelopmentAuthenticationHandler>("Development", _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = true;
    });
}
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapEnterpriseRagEndpoints();

if (app.Configuration.GetValue("Database:InitializeOnStartup", app.Environment.IsDevelopment()))
    await app.Services.InitializeDevelopmentDatabaseAsync();

app.Run();

public partial class Program;
