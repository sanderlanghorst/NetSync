using Microsoft.Extensions.DependencyInjection;

namespace NetSync;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetSync(this IServiceCollection services)
    {
        services.AddSingleton<ISerializer, JsonUtf8Serializer>();
        services.AddSingleton<Discovery>();
        services.AddSingleton<IMessaging, Messaging>();
        services.AddSingleton<ISyncData, SyncData>();
        services.AddHostedService<NetworkService>();
        return services;
    }
}