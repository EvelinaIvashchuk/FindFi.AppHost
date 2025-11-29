using FindFi.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Shared observability & discovery
builder.AddServiceDefaults();

// YARP with Service Discovery destination resolver
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseServiceDefaults();

// Map the reverse proxy to handle all requests
app.MapReverseProxy();

app.Run();
