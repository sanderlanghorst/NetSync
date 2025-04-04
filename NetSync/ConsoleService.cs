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

    public ConsoleService(IHostApplicationLifetime hostLifetime, IMessaging messaging)
    {
        _hostLifetime = hostLifetime;
        _messaging = messaging;
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
        Console.WriteLine("Available commands: get, set, list, help, exit");
    }

    private void Get(string s)
    {
    }

    private void Set(string s)
    {
    }

    private void List()
    {
        Console.WriteLine("Endpoints:");
        foreach (var endpoint in _messaging.EndPoints)
        {
            Console.WriteLine(endpoint);
        }
    }
}