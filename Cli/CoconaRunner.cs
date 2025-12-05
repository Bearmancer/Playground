using Cocona;

namespace Playground.Cli;

public sealed class CoconaRunner : ICliRunner
{
    public string Name => "Cocona";

    public sealed class Commands
    {
        [Command("scrape")]
        public void Scrape(
            [Option('v', Description = "Enable verbose output")] bool verbose = false,
            [Option('o', Description = "Output file path")] string? output = null
        )
        {
            SpectreLogger.Info($"[Cocona] Scrape: verbose={verbose}, output={output ?? "null"}");
        }

        [Command("mail")]
        public void Mail([Option('v', Description = "Enable verbose output")] bool verbose = false)
        {
            SpectreLogger.Info($"[Cocona] Mail: verbose={verbose}");
        }

        [Command("search")]
        public void Search(
            [Argument(Description = "Search query")] string query,
            [Option('t', Description = "Timeout in seconds")] int timeout = 30
        )
        {
            SpectreLogger.Info($"[Cocona] Search: query={query}, timeout={timeout}");
        }
    }

    public Task<int> RunAsync(string[] args)
    {
        try
        {
            CoconaApp.Run<Commands>(args);
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            SpectreLogger.Error($"[{Name}] Error: {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
