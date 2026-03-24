using InfoMap.Shared.Infrastructure.Extensions;
using Session.Shared.Infrastructure;
using Session.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureMassTransit();

builder.AddNpgsqlDbContext<SessionDbContext>("sessionDb");

builder.AddDefaultInfrastructure();
builder.AddInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultInfrastructure();
await app.UseInfrastructureAsync();

app.Run();
