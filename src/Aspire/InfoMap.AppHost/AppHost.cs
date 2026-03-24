using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitMQ = builder.AddRabbitMQ("rabbitmq");
var redis = builder.AddAzureManagedRedis("redis");
var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
       .WithPasswordAuthentication();

if (builder.Environment.IsDevelopment())
{
    postgres.RunAsContainer();
    redis.RunAsContainer();
}

var mapItemsDatabase = postgres.AddDatabase("mapitemsDb");
var chatDatabase = postgres.AddDatabase("chatDb");
var sessionDatabase = postgres.AddDatabase("sessionDb");


var mapitems = builder.AddProject<Projects.MapItems>("mapitems")
    .WithReference(mapItemsDatabase)
    .WaitFor(mapItemsDatabase)
    .WithReference(rabbitMQ)
    .WaitFor(rabbitMQ)
    .WithReference(redis)
    .WaitFor(redis);

var chat = builder.AddProject<Projects.Chat>("chat")
    .WithReference(chatDatabase)
    .WaitFor(chatDatabase)
    .WithReference(rabbitMQ)
    .WaitFor(rabbitMQ)
    .WithReference(redis)
    .WaitFor(redis);

var session = builder.AddProject<Projects.Session>("session")
    .WithReference(sessionDatabase)
    .WaitFor(sessionDatabase)
    .WithReference(rabbitMQ)
    .WaitFor(rabbitMQ)
    .WithReference(redis)
    .WaitFor(redis);

var apigateway = builder.AddProject<Projects.ApiGateway>("apigateway")
    .WithExternalHttpEndpoints()
    .WithReference(mapitems)
    .WithReference(chat)
    .WithReference(session)
    .WaitFor(mapitems)
    .WaitFor(chat)
    .WaitFor(session);

if (builder.Environment.IsDevelopment())
{
    apigateway.WithHttpsEndpoint(5001, name:"public");
}

builder.Build().Run();
