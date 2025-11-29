using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;

namespace FindFi.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Environment.ApplicationName;

        // Serilog configuration
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .WriteTo.Console(formatter: new Serilog.Formatting.Compact.CompactJsonFormatter())
            .CreateLogger();

        builder.Host.UseSerilog();

        // Service discovery
        builder.Services.AddServiceDiscovery();

        // OpenTelemetry tracing (HTTP, ASP.NET Core, SQL) + service custom sources
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName))
            .WithTracing(tracer =>
            {
                tracer
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddSource(serviceName);

                // OTLP exporter
                tracer.AddOtlpExporter();
            });

        return builder;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Ensure every request has a CorrelationId in logs and response header
        app.UseCorrelationId();

        app.UseSerilogRequestLogging();

        return app;
    }
}
