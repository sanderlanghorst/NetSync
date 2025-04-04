using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetSync;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ISerializer, BinarySerializer>();
        services.AddSingleton<IMessaging, Messaging>();
        services.AddSingleton<Discovery>();
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<INetworkService, NetworkService>();
    });
var host = builder.Build();
var console = host.Services.GetRequiredService<IConsoleService>();
var network = host.Services.GetRequiredService<INetworkService>();
var randomTask = Task.Run(async () =>
{
    await Task.Delay(new Random().Next(1000, 10000));
    await host.Services.GetRequiredService<IMessaging>().Send("Hi there", default);
});
await Task.WhenAll([console.Run(), network.Run(), randomTask]);
//new FirstService.FirstServiceClient()