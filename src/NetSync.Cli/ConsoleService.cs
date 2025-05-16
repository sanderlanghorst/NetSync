using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NetSync.Cli;

public class ConsoleService : IHostedService
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IMessaging _messaging;
    private readonly ISyncData _sync;
    private readonly IOptions<NetSyncOptions> _options;
    private CancellationTokenSource _cts = null!;
    private Task _task = null!;

    public ConsoleService(IHostApplicationLifetime hostLifetime, IMessaging messaging, ISyncData sync, IOptions<NetSyncOptions> options)
    {
        _hostLifetime = hostLifetime;
        _messaging = messaging;
        _sync = sync;
        _options = options;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500);
                var input = await Task.Run(() => Console.ReadLine(), cancellationToken);
                if (input == null)
                    continue;

                var parts = input.Split(' ', 2);
                switch (parts[0])
                {
                    case "get" when parts.Length > 1:
                        Get(parts[1]);
                        break;
                    case "set" when parts.Length > 1:
                        Set(parts[1]);
                        break;
                    case "list":
                        List();
                        break;
                    case "clients":
                        Clients();
                        break;
                    case "help":
                        Help();
                        break;
                    case "exit":
                        Exit();
                        break;
                    case "start" when _options.Value.ManualStart:
                        await Start();
                        break;
                    case "stop" when _options.Value.ManualStart:
                        await Stop();
                        break;
                    case "reset" when _options.Value.ManualStart:
                        await Reset();
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }


    private void Exit()
    {
        _hostLifetime.StopApplication();
    }

    private void Help()
    {
        Console.WriteLine("Available commands: get, set, list, clients, help, exit");
    }
    private class ConsoleMessage
    {
        public required string Message { get; set; }
    }

    private void Get(string key)
    {
        var value = _sync.Get<ConsoleMessage>(key);
        Console.WriteLine(value?.Message);
    }

    private void Set(string s)
    {
        var split = s.Split(":");
        if (split.Length != 2)
        {
            Console.WriteLine("Separate key and value with ':'");
            return;
        }

        _sync.Set(split[0], new ConsoleMessage{Message = split[1]});
    }

    private void List()
    {
        Console.WriteLine("Keys:");
        foreach (var keys in _sync.List())
        {
            Console.WriteLine(keys);
        }
    }

    private void Clients()
    {
        Console.WriteLine("Endpoints:");
        foreach (var endpoint in _messaging.EndPoints)
        {
            Console.WriteLine(endpoint);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _task = Run(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();
        await Task.WhenAll(_task);
    }

    private async Task Start()
    {
        _options.Value.Start?.Invoke(_cts.Token);
    }
    private async Task Stop()
    {
        _options.Value.Stop?.Invoke();
    }
    private async Task Reset()
    {
        _messaging.EndPoints.Clear();
        _sync.Clear();
    }
}