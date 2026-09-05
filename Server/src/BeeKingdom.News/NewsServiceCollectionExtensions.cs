using BeeKingdom.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.News;

public static class NewsServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomNews(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NewsOptions>()
            .Bind(configuration.GetSection(NewsOptions.SectionName))
            .Validate(o => { o.Validate(); return true; })
            .ValidateOnStart();

        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<INewsRepository, SqlNewsRepository>();
        }
        else
        {
            // Non-SQL local/dev environments only - production runs Persistence:Provider=SqlServer
            // (see SqlNewsRepository's class comment). Not durable across a process restart.
            services.AddSingleton<INewsRepository, InMemoryNewsRepository>();
        }

        services.AddSingleton<NewsService>();

        return services;
    }
}
