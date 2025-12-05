using System.CommandLine;
using System.CommandLine.Parsing;

namespace Playground.Cli;

public sealed class SystemCommandLineRunner : ICliRunner
{
    public string Name => "System.CommandLine";

    public Task<int> RunAsync(string[] args)
    {
        Option<bool> verboseOption = new("-v", "--verbose")
        {
            Description = "Enable verbose output",
        };
        Option<string?> outputOption = new("-o", "--output") { Description = "Output file path" };
        Option<int> timeoutOption = new("-t", "--timeout")
        {
            Description = "Timeout in seconds",
            DefaultValueFactory = _ => 30,
        };
        Argument<string> queryArgument = new("query") { Description = "Search query" };

        Command scrapeCommand = new("scrape", "Scrape Bowie discography")
        {
            verboseOption,
            outputOption,
        };
        scrapeCommand.SetAction(parseResult =>
        {
            bool verbose = parseResult.GetValue(verboseOption);
            string? output = parseResult.GetValue(outputOption);
            SpectreLogger.Info($"[{Name}] Scrape: verbose={verbose}, output={output ?? "null"}");
            return 0;
        });

        Command mailCommand = new("mail", "Test mail.tm service") { verboseOption };
        mailCommand.SetAction(parseResult =>
        {
            bool verbose = parseResult.GetValue(verboseOption);
            SpectreLogger.Info($"[{Name}] Mail: verbose={verbose}");
            return 0;
        });

        Command searchCommand = new("search", "Search music metadata")
        {
            queryArgument,
            timeoutOption,
        };
        searchCommand.SetAction(parseResult =>
        {
            string query = parseResult.GetValue(queryArgument) ?? "";
            int timeout = parseResult.GetValue(timeoutOption);
            SpectreLogger.Info($"[{Name}] Search: query={query}, timeout={timeout}");
            return 0;
        });

        RootCommand rootCommand = new("Playground CLI using System.CommandLine")
        {
            scrapeCommand,
            mailCommand,
            searchCommand,
        };

        ParseResult parseResult = rootCommand.Parse(args);
        return Task.FromResult(parseResult.Invoke());
    }
}
