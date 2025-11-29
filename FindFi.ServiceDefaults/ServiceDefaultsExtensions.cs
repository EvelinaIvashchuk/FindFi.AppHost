using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace FindFi.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        // Serilog configuration
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .WriteTo.Console(formatter: new Serilog.Formatting.Compact.CompactJsonFormatter())
            .CreateLogger();

        builder.Host.UseSerilog();

        // Service discovery
        builder.Services.AddServiceDiscovery();

        // OpenTelemetry tracing (HTTP, ASP.NET Core, SQL)
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracer =>
            {
                tracer
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation();

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

        app.UseSerilogRequestLogging();

        return app;
    }
}
