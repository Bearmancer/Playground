using CommandLine;

namespace Playground.Cli;

public sealed class CommandLineParserRunner : ICliRunner
{
    public string Name => "CommandLineParser";

    [Verb("scrape", HelpText = "Scrape Bowie discography")]
    public sealed class ScrapeOptions
    {
        [Option('v', "verbose", Default = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }

        [Option('o', "output", HelpText = "Output file path")]
        public string? Output { get; set; }
    }

    [Verb("mail", HelpText = "Test mail.tm service")]
    public sealed class MailOptions
    {
        [Option('v', "verbose", Default = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
    }

    [Verb("search", HelpText = "Search music metadata")]
    public sealed class SearchOptions
    {
        [Value(0, Required = true, MetaName = "query", HelpText = "Search query")]
        public string Query { get; set; } = "";

        [Option('t', "timeout", Default = 30, HelpText = "Timeout in seconds")]
        public int Timeout { get; set; }
    }

    public Task<int> RunAsync(string[] args)
    {
        ParserResult<object> result = Parser.Default.ParseArguments<
            ScrapeOptions,
            MailOptions,
            SearchOptions
        >(args);

        return Task.FromResult(
            result.MapResult(
                (ScrapeOptions opts) =>
                {
                    SpectreLogger.Info(
                        $"[{Name}] Scrape: verbose={opts.Verbose}, output={opts.Output ?? "null"}"
                    );
                    return 0;
                },
                (MailOptions opts) =>
                {
                    SpectreLogger.Info($"[{Name}] Mail: verbose={opts.Verbose}");
                    return 0;
                },
                (SearchOptions opts) =>
                {
                    SpectreLogger.Info(
                        $"[{Name}] Search: query={opts.Query}, timeout={opts.Timeout}"
                    );
                    return 0;
                },
                errors => 1
            )
        );
    }
}
