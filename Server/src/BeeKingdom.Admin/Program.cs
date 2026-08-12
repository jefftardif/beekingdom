using BeeKingdom.Infrastructure.Configuration;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Infrastructure.Hosting;
using BeeKingdom.Persistence.DependencyInjection;
using BeeKingdom.Persistence.Migrations;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services
    .AddBeeKingdomInfrastructure(builder.Configuration)
    .AddBeeKingdomPersistence(builder.Configuration);

WebApplication app = builder.Build();

app.MapGet("/", () => Results.Redirect("/admin/status"));

app.MapGet("/admin/status", async (IOptions<BeeKingdomServerOptions> options, IOptions<BeeKingdomServerHostProfile> serverProfile, IMigrationRunner migrations, CancellationToken cancellationToken) =>
{
    return Results.Ok(new
    {
        service = $"{options.Value.ServerName}.Admin",
        liveOps = "Ready",
        hosting = serverProfile.Value.HostingModel,
        sqlServerRole = serverProfile.Value.SqlServerRole,
        pendingMigrations = await migrations.GetPendingMigrationsAsync(cancellationToken)
    });
});

app.Run();
