using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace FindFi.ServiceDefaults;

public static class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                // Fall back to ASP.NET Core trace identifier if not provided
                correlationId = context.TraceIdentifier;
            }

            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
    }
}
