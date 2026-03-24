using Chat.Shared.Infrastructure;
using Chat.Shared.Infrastructure.Extensions;
using Chat.Features.Consumers;
using InfoMap.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureMassTransit(m =>
{
    m.AddConsumer<SessionCreatedConsumerChat>();
    m.AddConsumer<SessionDeletedConsumerChat>();
});

builder.AddNpgsqlDbContext<ChatDbContext>("chatDb");

builder.AddDefaultInfrastructure();
builder.AddInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultInfrastructure();
await app.UseInfrastructureAsync();

app.Run();
