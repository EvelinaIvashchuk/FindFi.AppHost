using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Register orchestrated projects
var aggregator = builder.AddProject<FindFi_Aggregator>("aggregator");
var gateway = builder.AddProject<FindFi_ApiGateway>("apigateway")
    .WithReference(aggregator);

builder.Build().Run();