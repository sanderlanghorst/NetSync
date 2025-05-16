using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetSync;
using NetSync.Cli;

var manualMode = args.Contains("--manual");
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
        services.AddNetSync(o => o.ManualStart = manualMode);
        services.AddHostedService<ConsoleService>();
    });
var host = builder.Build();
await host.RunAsync();
