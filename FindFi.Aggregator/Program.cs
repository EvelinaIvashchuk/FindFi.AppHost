using FindFi.ServiceDefaults;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register HttpClients pointing to locally hosted services
// bookingService -> http://localhost:5021
builder.Services.AddHttpClient("bookingService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5021");
});

// listingService -> http://localhost:5022
builder.Services.AddHttpClient("listingService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5022");
});

var app = builder.Build();

app.UseServiceDefaults();

app.MapGet("/aggregate", async (IHttpClientFactory factory) =>
{
    var listing = factory.CreateClient("listingService");
    var booking = factory.CreateClient("bookingService");

    // Placeholder calls; microservices may not be present during initial setup
    try
    {
        var listingCount = await listing.GetFromJsonAsync<int>("api/metrics/listing-count");
        var bookingCount = await booking.GetFromJsonAsync<int>("api/metrics/booking-count");
        return Results.Ok(new { listingCount, bookingCount });
    }
    catch
    {
        return Results.Ok(new { message = "Aggregator is configured. Downstream services not currently available." });
    }
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
