using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Databases
// SQL Server with persistent volume
var sql = builder
    .AddSqlServer("sql")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", "12345678")
    .WithDataVolume();

var sqlDb = sql.AddDatabase("FindFiDb");

// MongoDB with its own persistent volume
var mongo = builder
    .AddMongoDB("mongo")
    .WithDataVolume();

var mongoDb = mongo.AddDatabase("FindFiMongo");

// Register orchestrated projects
var aggregator = builder.AddProject<FindFi_Aggregator>("aggregator")
    // Provide DB connection strings via environment ConnectionStrings:*
    .WithReference(sqlDb, "FindFiDb")
    .WithReference(mongoDb, "FindFiMongo")
    // Inject required external DB settings from issue description
    .WithEnvironment("ConnectionStrings:rent_core", "Server=localhost;Port=3306;Database=rent_core;User ID=root;Password=12345678;SslMode=None;")
    .WithEnvironment("MongoDb:ConnectionString", "mongodb://localhost:27017")
    .WithEnvironment("MongoDb:DatabaseName", "reviews_core")
    // Ensure DBs are ready before starting the aggregator
    .WaitFor(sql)
    .WaitFor(mongo);

var gateway = builder.AddProject<FindFi_ApiGateway>("apigateway")
    // Fixed HTTP port for convenient local development(port: 6000)
    // Gateway needs to know about downstream services for routing
    .WithReference(aggregator)
    // Start after the aggregator
    .WaitFor(aggregator);

builder.Build().Run();