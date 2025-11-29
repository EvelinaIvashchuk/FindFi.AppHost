using FindFi.ServiceDefaults;
using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Swagger/OpenAPI for contract visibility
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClients using service discovery logical names defined in AppHost
// Aspire injects service URLs via service discovery, so we can use DNS names
builder.Services.AddHttpClient("bookingService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5021");
});

builder.Services.AddHttpClient("listingService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5022");
});

builder.Services.AddHttpClient("reviewsService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5064");
});

// ActivitySource for custom business spans
var activitySource = new ActivitySource(builder.Environment.ApplicationName);

var sqlConn = builder.Configuration.GetConnectionString("FindFiDb")
             ?? builder.Configuration.GetConnectionString("SqlServerDb");
var mongoConn = builder.Configuration.GetConnectionString("FindFiMongo")
               ?? builder.Configuration.GetConnectionString("MongoDb");
var mySqlConn = builder.Configuration.GetConnectionString("rent_core");

var app = builder.Build();

app.UseServiceDefaults();

app.UseSwagger();
app.UseSwaggerUI();

// Backward-compatible route
app.MapGet("/aggregate", async (IHttpClientFactory factory, ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("Aggregator");
    using var activity = activitySource.StartActivity("AggregateMetrics", ActivityKind.Internal);

    var listing = factory.CreateClient("listingService");
    var booking = factory.CreateClient("bookingService");
    var reviews = factory.CreateClient("reviewsService");

    try
    {
        var listingCount = await listing.GetFromJsonAsync<int>("api/metrics/listing-count");
        var bookingCount = await booking.GetFromJsonAsync<int>("api/metrics/booking-count");
        var reviewsCount = await reviews.GetFromJsonAsync<int>("api/metrics/reviews-count");

        logger.LogInformation("Aggregated metrics with counts: ListingCount={ListingCount}, BookingCount={BookingCount}, ReviewsCount={ReviewsCount}", listingCount, bookingCount, reviewsCount);
        return Results.Ok(new { listingCount, bookingCount, reviewsCount });
    }
    catch (HttpRequestException ex)
    {
        logger.LogWarning(ex, "Downstream services unavailable while aggregating metrics");
        return Results.Problem(title: "Downstream services unavailable", statusCode: 503);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during aggregation");
        return Results.Problem(title: "Unexpected error", statusCode: 500);
    }
});

// Preferred API-prefixed route
app.MapGet("/api/aggregator/aggregate", async (IHttpClientFactory factory, ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("Aggregator");
    using var activity = activitySource.StartActivity("AggregateMetrics", ActivityKind.Internal);

    var listing = factory.CreateClient("listingService");
    var booking = factory.CreateClient("bookingService");
    var reviews = factory.CreateClient("reviewsService");

    try
    {
        var listingCount = await listing.GetFromJsonAsync<int>("api/metrics/listing-count");
        var bookingCount = await booking.GetFromJsonAsync<int>("api/metrics/booking-count");
        var reviewsCount = await reviews.GetFromJsonAsync<int>("api/metrics/reviews-count");

        logger.LogInformation("Aggregated metrics with counts: ListingCount={ListingCount}, BookingCount={BookingCount}, ReviewsCount={ReviewsCount}", listingCount, bookingCount, reviewsCount);
        return Results.Ok(new { listingCount, bookingCount, reviewsCount });
    }
    catch (HttpRequestException ex)
    {
        logger.LogWarning(ex, "Downstream services unavailable while aggregating metrics");
        return Results.Problem(title: "Downstream services unavailable", statusCode: 503);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during aggregation");
        return Results.Problem(title: "Unexpected error", statusCode: 500);
    }
});

app.MapGet("/health", () => Results.Ok("ok"));
app.MapGet("/api/aggregator/health", () => Results.Ok("ok"));

// Log configured connection strings presence (without sensitive data)
app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("ConnectionStrings configured: FindFiDb={HasSql}, FindFiMongo={HasMongo}, rent_core={HasMySql}",
        !string.IsNullOrEmpty(sqlConn), !string.IsNullOrEmpty(mongoConn), !string.IsNullOrEmpty(mySqlConn));
});

app.Run();
