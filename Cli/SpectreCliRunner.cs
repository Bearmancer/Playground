using Spectre.Console.Cli;

namespace Playground.Cli;

public sealed class SpectreCliRunner : ICliRunner
{
    public string Name => "Spectre.Console.Cli";

    public sealed class ScrapeCommand : Command<ScrapeCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("-v|--verbose")]
            public bool Verbose { get; set; }

            [CommandOption("-o|--output")]
            public string? Output { get; set; }
        }

        public override int Execute(
            CommandContext context,
            Settings settings,
            CancellationToken cancellationToken
        )
        {
            SpectreLogger.Info(
                $"[Spectre.Console.Cli] Scrape: verbose={settings.Verbose}, output={settings.Output ?? "null"}"
            );
            return 0;
        }
    }

    public sealed class MailCommand : Command<MailCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("-v|--verbose")]
            public bool Verbose { get; set; }
        }

        public override int Execute(
            CommandContext context,
            Settings settings,
            CancellationToken cancellationToken
        )
        {
            SpectreLogger.Info($"[Spectre.Console.Cli] Mail: verbose={settings.Verbose}");
            return 0;
        }
    }

    public sealed class SearchCommand : Command<SearchCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "<query>")]
            public string Query { get; set; } = "";

            [CommandOption("-t|--timeout")]
            public int Timeout { get; set; } = 30;
        }

        public override int Execute(
            CommandContext context,
            Settings settings,
            CancellationToken cancellationToken
        )
        {
            SpectreLogger.Info(
                $"[Spectre.Console.Cli] Search: query={settings.Query}, timeout={settings.Timeout}"
            );
            return 0;
        }
    }

    public Task<int> RunAsync(string[] args)
    {
        CommandApp app = new();
        app.Configure(config =>
        {
            config.AddCommand<ScrapeCommand>("scrape").WithDescription("Scrape Bowie discography");
            config.AddCommand<MailCommand>("mail").WithDescription("Test mail.tm service");
            config.AddCommand<SearchCommand>("search").WithDescription("Search music metadata");
        });

        return Task.FromResult(app.Run(args));
    }
}
