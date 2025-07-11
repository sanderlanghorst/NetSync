using Microsoft.Extensions.DependencyInjection;

namespace NetSync;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetSync(this IServiceCollection services, Action<NetSyncOptions>? configure = null)
    {
        services.AddSingleton<ISerializer, JsonUtf8Serializer>();
        services.AddSingleton<IDiscovery, Discovery>();
        services.AddSingleton<IMessaging, Messaging>();
        services.AddSingleton<ISyncData, SyncData>();
        services.AddHostedService<NetworkService>();
        if (configure is not null)
        {
            services.Configure(configure);
        }
        return services;
    }
}