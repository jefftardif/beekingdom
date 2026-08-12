using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Persistence.DependencyInjection;
using BeeKingdom.Persistence.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddBeeKingdomInfrastructure(builder.Configuration)
    .AddBeeKingdomPersistence(builder.Configuration);

using IHost host = builder.Build();

string command = args.Length > 0 ? args[0].ToLowerInvariant() : "diagnostics";

switch (command)
{
    case "migrate":
        await host.Services.GetRequiredService<IMigrationRunner>().ApplyPendingMigrationsAsync();
        Console.WriteLine("Migration scripts registered.");
        break;

    case "diagnostics":
        IReadOnlyList<string> pending = await host.Services.GetRequiredService<IMigrationRunner>().GetPendingMigrationsAsync();
        Console.WriteLine($"Bee Kingdom server tools ready. Registered migrations: {pending.Count}.");
        break;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        Environment.ExitCode = 2;
        break;
}
