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
        bool exit = false;
        while (!exit && !_hostLifetime.ApplicationStopping.IsCancellationRequested)
        {
            await Task.Delay(500);
            var input = Console.ReadLine();
            if (input == null)
                continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                exit = true;
                _hostLifetime.StopApplication();
            }
            else if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Available commands: exit, help, say");
            }
            else if (input.StartsWith("say", StringComparison.OrdinalIgnoreCase))
            {
                var say = input.Substring(3).Trim();
                await _messaging.Send(say, _hostLifetime.ApplicationStopping);
            }
            else if(input.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Endpoints:");
                foreach (var endpoint in _messaging.EndPoints)
                {
                    Console.WriteLine(endpoint);
                }
            }
            else
            {
                Console.WriteLine($"Unknown command: {input}");
            }
        }
    }
}