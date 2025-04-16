using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetSync;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(logging =>
        {
            logging.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "hh:mm:ss ";
            });
        });
        services.AddSingleton<ISerializer, JsonUtf8Serializer>();
        services.AddSingleton<ISyncData, SyncData>();
        services.AddSingleton<IMessaging, Messaging>();
        services.AddSingleton<Discovery>();
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<INetworkService, NetworkService>();
    });
var host = builder.Build();
var console = host.Services.GetRequiredService<IConsoleService>();
var network = host.Services.GetRequiredService<INetworkService>();

await Task.WhenAll(console.Run(), network.Run());
//new FirstService.FirstServiceClient()