namespace Playground;

internal sealed class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            CliHelper.ShowHelp();
            return 0;
        }

        CommandLineParser parser = CommandLineParser.Parse(args);

        return parser.Command?.ToLower() switch
        {
            "help" => ShowHelp(parser),
            "scrape" => await RunScraper(parser),
            "mail" => await RunMail(parser),
            "metadata" => await RunMetadata(parser),
            "cli" => await RunCliComparison(parser),
            _ => ShowUnknown(parser.Command),
        };
    }

    static int ShowHelp(CommandLineParser parser)
    {
        if (parser.Arguments.Count > 0)
            CliHelper.ShowCommandHelp(parser.Arguments[0]);
        else
            CliHelper.ShowHelp();
        return 0;
    }

    static async Task<int> RunScraper(CommandLineParser parser)
    {
        SpectreLogger.Info("Scraper command selected");
        SpectreLogger.KeyValue("Verbose", parser.HasOption("verbose").ToString());
        SpectreLogger.Warning("Scraper not yet fully implemented in refactored Program.cs");
        await Task.CompletedTask;
        return 0;
    }

    static async Task<int> RunMail(CommandLineParser parser)
    {
        SpectreLogger.Rule("Mail.tm Service");

        MailTmService service = new();

        try
        {
            MailTmAccount account = await service.CreateAccountAsync();
            SpectreLogger.Success($"Created account: {account.Address}");

            List<MailTmMessage> inbox = await service.GetInboxAsync();
            SpectreLogger.Info($"Inbox has {inbox.Count} messages");

            await service.DeleteAccountAsync();
            SpectreLogger.Success("Account deleted");

            return 0;
        }
        catch (MailTmException ex)
        {
            SpectreLogger.Error(ex.Message);
            return 1;
        }
    }

    static async Task<int> RunMetadata(CommandLineParser parser)
    {
        SpectreLogger.Rule("Music Metadata Search");

        string title = parser.GetOption("title") ?? "Heroes";
        string? artist = parser.GetOption("artist") ?? "David Bowie";
        string? discogsToken = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN");

        MusicMetadataService service = new(discogsToken);

        MusicSearchResult? result = await service.SearchAsync(new(Artist: artist, Album: title));

        if (result is null)
        {
            SpectreLogger.Warning("No results found");
            return 1;
        }

        Table table = new();
        table.AddColumn("Field");
        table.AddColumn("Value");
        table.AddRow("Title", result.Title);
        table.AddRow("Artist", result.Artist);
        table.AddRow("Year", result.Year?.ToString() ?? "Unknown");
        table.AddRow("Source", result.Source);
        table.AddRow("External ID", result.ExternalId);

        AnsiConsole.Write(table);

        return 0;
    }

    static async Task<int> RunCliComparison(CommandLineParser parser)
    {
        SpectreLogger.Rule("CLI Framework Comparison");

        ICliRunner[] runners = CliComparison.GetAllRunners();
        string[] testArgs = ["scrape", "--verbose"];

        foreach (ICliRunner runner in runners)
        {
            SpectreLogger.Starting($"Testing: {runner.Name}");
            int result = await runner.RunAsync(testArgs);
            string status = result == 0 ? "Success" : "Failed";
            SpectreLogger.KeyValue(runner.Name, status);
        }

        return 0;
    }

    static int ShowUnknown(string? command)
    {
        SpectreLogger.Error($"Unknown command: {command ?? "(none)"}");
        CliHelper.ShowHelp();
        return 1;
    }
}
