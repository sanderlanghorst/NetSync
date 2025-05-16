using System.Diagnostics;

namespace NetSync.Tests;

public class ApplicationFixture : IAsyncLifetime
{
    private readonly List<ConsoleApplication> _consoleApplications = [];
    public IReadOnlyList<ConsoleApplication> Consoles => _consoleApplications;


    public async Task InitializeAsync()
    {
        string exePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "NetSync.Cli", "bin", "Debug",
                "net9.0", "NetSync.Cli.exe")
        );
        for (int i = 0; i < 3; i++)
        {
            var app = new ConsoleApplication(exePath);
            await app.StartAsync();
            _consoleApplications.Add(app);
        }

        await Task.Delay(1000);
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_consoleApplications.Select(async c => await c.DisposeAsync()));
    }

    public class ConsoleApplication
    {
        private readonly Process _process;

        public ConsoleApplication(string exePath)
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            _process.OutputDataReceived += (sender, args) => Logs.Add(DateTime.Now, args.Data);
        }

        public IDictionary<DateTime, string?> Logs { get; } = new Dictionary<DateTime, string?>();

        public async Task StartAsync()
        {
            _process.Start();
            _process.BeginOutputReadLine();
        }

        public async Task DisposeAsync()
        {
            if (!_process.HasExited)
            {
                _process.CancelOutputRead();
                _process.Kill();
                await _process.WaitForExitAsync();
            }

            _process.Dispose();
        }

        public async Task Write(string input)
        {
            await _process.StandardInput.WriteLineAsync(input);
        }

        public async Task<string> Read(DateTime since, int takeLines = 0)
        {
            int maxTries = 5;
            do
            {
                var lines = takeLines <= 0
                    ? Logs.Where(x => x.Key > since).Select(x => x.Value).ToArray()
                    : Logs.Where(x => x.Key > since).Select(x => x.Value).Take(takeLines).ToArray();
                if (lines.Any())
                {
                    return string.Join(Environment.NewLine, lines);
                }

                await Task.Delay(500);
            } while (maxTries-- > 0);

            return string.Empty;
        }
    }

    public async Task ResetState()
    {
        foreach (var console in _consoleApplications)
        {
            await console.Write("reset");
            console.Logs.Clear();
        }
    }
}