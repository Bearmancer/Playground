using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace Playground.Cli;

public sealed class CliFxRunner : ICliRunner
{
    public string Name => "CliFx";

    [Command("scrape", Description = "Scrape Bowie discography")]
    public class ScrapeCommand : ICommand
    {
        [CommandOption("verbose", 'v', Description = "Enable verbose output")]
        public bool Verbose { get; init; }

        [CommandOption("output", 'o', Description = "Output file path")]
        public string? Output { get; init; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            SpectreLogger.Info($"[CliFx] Scrape: verbose={Verbose}, output={Output ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("mail", Description = "Test mail.tm service")]
    public class MailCommand : ICommand
    {
        [CommandOption("verbose", 'v', Description = "Enable verbose output")]
        public bool Verbose { get; init; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            SpectreLogger.Info($"[CliFx] Mail: verbose={Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("search", Description = "Search music metadata")]
    public class SearchCommand : ICommand
    {
        [CommandParameter(0, Description = "Search query")]
        public string Query { get; init; } = "";

        [CommandOption("timeout", 't', Description = "Timeout in seconds")]
        public int Timeout { get; init; } = 30;

        public ValueTask ExecuteAsync(IConsole console)
        {
            SpectreLogger.Info($"[CliFx] Search: query={Query}, timeout={Timeout}");
            return ValueTask.CompletedTask;
        }
    }

    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            await new CliApplicationBuilder()
                .AddCommand<ScrapeCommand>()
                .AddCommand<MailCommand>()
                .AddCommand<SearchCommand>()
                .Build()
                .RunAsync(args);
            return 0;
        }
        catch (Exception ex)
        {
            SpectreLogger.Error($"[{Name}] Error: {ex.Message}");
            return 1;
        }
    }
}
