using MapItems.Shared.Infrastructure;
using MapItems.Shared.Infrastructure.Extensions;
using MapItems.Features.Consumers;
using InfoMap.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureMassTransit(m =>
{
    m.AddConsumer<SessionCreatedConsumerMapItems>();
    m.AddConsumer<SessionDeletedConsumerMapItems>();
});

builder.AddNpgsqlDbContext<MapItemsDbContext>("mapitemsDb");

builder.AddDefaultInfrastructure();
builder.AddInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultInfrastructure();
await app.UseInfrastructureAsync();


app.Run();
