using Microsoft.Extensions.DependencyInjection;

namespace EventStore.MongoDb;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MongoDb EventStore services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure MongoDbEventStoreOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMongoDbEventStore(
        this IServiceCollection services,
        Action<MongoDbEventStoreOptions> configureOptions
    )
    {
        services.Configure(configureOptions);

        services.AddSingleton<IEventStore, MongoDbEventStore>();

        return services;
    }
}
