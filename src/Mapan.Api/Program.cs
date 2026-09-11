using Mapan.Api;
using Mapan.Application;
using Mapan.Infrastructure;
using Mapan.Infrastructure.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
// Console logging works without Windows Event Log write privileges.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
// Development uses JWTs, not persisted Data Protection cookies. Avoid sharing
// another Windows account's DPAPI key ring when running local tooling.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    builder.Services.Configure<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>(options =>
    {
        options.XmlRepository = new DevelopmentKeyRepository();
        options.XmlEncryptor = new Microsoft.AspNetCore.DataProtection.XmlEncryption.NullXmlEncryptor();
    });
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMapanSecurity(builder.Configuration);
builder.Services.AddMapanApi(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) => {
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
