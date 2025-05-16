using Microsoft.Extensions.DependencyInjection;

namespace NetSync;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetSync(this IServiceCollection services)
    {
        return AddNetSync(services, _ => { });
    }
    public static IServiceCollection AddNetSync(this IServiceCollection services, Action<NetSyncOptions> configure)
    {
        services.AddSingleton<ISerializer, JsonUtf8Serializer>();
        services.AddSingleton<Discovery>();
        services.AddSingleton<IMessaging, Messaging>();
        services.AddSingleton<ISyncData, SyncData>();
        services.AddHostedService<NetworkService>();
        services.Configure(configure);
        return services;
    }
}