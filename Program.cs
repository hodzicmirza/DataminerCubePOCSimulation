using System.Xml.Serialization;
using LinuxServerDataminerPOC.Application.Dtos;
using LinuxServerDataminerPOC.Application.Interfaces;
using LinuxServerDataminerPOC.Application.Options;
using LinuxServerDataminerPOC.Application.Services;
using LinuxServerDataminerPOC.Domain.Interfaces;
using LinuxServerDataminerPOC.Infrastructure.Collectors;
using LinuxServerDataminerPOC.Infrastructure.Health;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting LinuxServerDataminerPOC service...");

    builder.Services.Configure<MetricsOptions>(
        builder.Configuration.GetSection(MetricsOptions.SectionName));

    builder.Services.AddScoped<ILinuxMetricsCollector, RealLinuxMetricsCollector>();
    builder.Services.AddScoped<IMetricsService, MetricsService>();

    builder.Services.AddHealthChecks()
        .AddCheck<ServerHealthCheck>("linux_health_check");

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.MapGet("/api/metrics", async (IMetricsService metricsService, CancellationToken ct) =>
    {
        var metrics = await metricsService.GetCurrentMetricsAsync(ct);
        return Results.Ok(metrics);
    });

    app.MapGet("/api/metrics/xml", async (IMetricsService metricsService, CancellationToken ct) =>
    {
        var metrics = await metricsService.GetCurrentMetricsAsync(ct);
        
        var serializer = new XmlSerializer(typeof(ServerMetricsDto));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, metrics);

        return Results.Content(stringWriter.ToString(), "application/xml");
    });

    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
