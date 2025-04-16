using Microsoft.Extensions.Hosting;

namespace NetSync;

public interface IConsoleService
{
    Task Run();
}

public class ConsoleService : IConsoleService
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IMessaging _messaging;
    private readonly ISyncData _sync;

    public ConsoleService(IHostApplicationLifetime hostLifetime, IMessaging messaging, ISyncData sync)
    {
        _hostLifetime = hostLifetime;
        _messaging = messaging;
        _sync = sync;
    }

    public async Task Run()
    {
        while (!_hostLifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500);
                var input = Console.ReadLine();
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
                }
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
        public string Message { get; set; }
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
}