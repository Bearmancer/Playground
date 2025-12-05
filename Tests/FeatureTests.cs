using Spectre.Console;

namespace Playground.Tests;

public record TestCase(string Name, Func<Task<bool>> Execute);

public record TestResult(string Name, bool Passed, TimeSpan Duration, string? Error = null);

public static class TestRunner
{
    public static async Task<List<TestResult>> RunAllAsync(List<TestCase> tests)
    {
        List<TestResult> results = [];
        
        AnsiConsole.Write(new Rule("[bold blue]Test Battery[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                ProgressTask progressTask = ctx.AddTask("[cyan]Running Tests[/]", maxValue: tests.Count);

                foreach (TestCase test in tests)
                {
                    DateTime start = DateTime.UtcNow;
                    progressTask.Description = $"[cyan]{test.Name}[/]";
                    
                    try
                    {
                        bool passed = await test.Execute();
                        TimeSpan duration = DateTime.UtcNow - start;
                        results.Add(new TestResult(test.Name, passed, duration));
                    }
                    catch (Exception ex)
                    {
                        TimeSpan duration = DateTime.UtcNow - start;
                        results.Add(new TestResult(test.Name, false, duration, ex.Message));
                    }
                    
                    progressTask.Increment(1);
                }
            });

        AnsiConsole.WriteLine();
        DisplayResults(results);
        return results;
    }

    static void DisplayResults(List<TestResult> results)
    {
        Table table = new();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[bold]Test[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Duration[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Error[/]").LeftAligned());

        foreach (TestResult result in results)
        {
            string status = result.Passed ? "[green]PASS[/]" : "[red]FAIL[/]";
            string duration = $"{result.Duration.TotalMilliseconds:F0}ms";
            string error = result.Error ?? "";
            table.AddRow(result.Name, status, duration, error.Length > 50 ? error[..50] + "..." : error);
        }

        AnsiConsole.Write(table);

        int passed = results.Count(r => r.Passed);
        int failed = results.Count - passed;
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Summary:[/] [green]{passed} passed[/], [red]{failed} failed[/]");
    }
}

public static class FeatureTests
{
    public static List<TestCase> GetAllTests() =>
    [
        new("MailTm.CreateAccount", async () =>
        {
            MailTmService service = new();
            MailTmAccount account = await service.CreateAccountAsync();
            await service.DeleteAccountAsync();
            return account.Address.Contains('@') && !string.IsNullOrEmpty(account.Id);
        }),
        
        new("MailTm.GetInboxUnauth", async () =>
        {
            MailTmService service = new();
            try
            {
                await service.GetInboxAsync();
                return false;
            }
            catch (MailTmException ex)
            {
                return ex.Message.Contains("Not authenticated");
            }
        }),
        
        new("MusicBrainz.SearchSpaceOddity", async () =>
        {
            MusicMetadataService service = new();
            MusicSearchResult result = await service.SearchAsync("Space Oddity", "David Bowie")
                ?? throw new Exception("Expected result");
            return result.Source == "MusicBrainz" && result.Title.Contains("Space Oddity", StringComparison.OrdinalIgnoreCase);
        }),
        
        new("MusicBrainz.SearchHeroes", async () =>
        {
            MusicMetadataService service = new();
            MusicSearchResult result = await service.SearchAsync("Heroes", "David Bowie")
                ?? throw new Exception("Expected result");
            return result.Title.Contains("Heroes", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(result.ExternalId);
        }),
        
        new("MusicBrainz.NonexistentReturnsNull", async () =>
        {
            MusicMetadataService service = new();
            MusicSearchResult? result = await service.SearchAsync("xyznonexistent123456789", "FakeArtist");
            return result is null;
        }),
        
        new("Discogs.RequiresToken", async () =>
        {
            string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
                ?? throw new Exception("DISCOGS_USER_TOKEN not set");
            MusicMetadataService service = new(token);
            MusicSearchResult result = await service.SearchAsync("Heroes", "David Bowie")
                ?? throw new Exception("Expected result");
            return !string.IsNullOrEmpty(result.Title);
        }),
        
        new("Discogs.SearchAsync", async () =>
        {
            string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
                ?? throw new Exception("DISCOGS_USER_TOKEN not set");
            DiscogsService service = new(new DiscogsClientConfig(token));
            List<DiscogsSearchResult> results = await service.SearchAsync(artist: "David Bowie", maxResults: 5);
            return results.Count > 0;
        }),
        
        new("Discogs.GetRelease", async () =>
        {
            string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
                ?? throw new Exception("DISCOGS_USER_TOKEN not set");
            DiscogsService service = new(new DiscogsClientConfig(token));
            DiscogsRelease? release = await service.GetReleaseAsync(249504);
            return release is { } r && r.Title.Length > 0;
        }),
        
        new("Discogs.GetMaster", async () =>
        {
            string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
                ?? throw new Exception("DISCOGS_USER_TOKEN not set");
            DiscogsService service = new(new DiscogsClientConfig(token));
            DiscogsMaster? master = await service.GetMasterAsync(23420);
            return master is { } m && m.Title.Length > 0;
        }),
        
        new("SpectreLogger.SpecialChars", () =>
        {
            string[] messages = ["{\"id\":\"123\"}", "data[index]", "C:\\Program Files\\[app]"];
            foreach (string msg in messages)
            {
                SpectreLogger.Info(msg);
                SpectreLogger.Warning(msg);
                SpectreLogger.Error(msg);
            }
            return Task.FromResult(true);
        }),
        
        new("SpectreLogger.Table", () =>
        {
            Dictionary<string, string> data = new()
            {
                ["Key[0]"] = "Value[brackets]",
                ["Normal"] = "Value"
            };
            SpectreLogger.Table("Test", data);
            return Task.FromResult(true);
        }),
        
        new("CommandLineParser.ParseCommand", () =>
        {
            CommandLineParser parser = CommandLineParser.Parse(["scrape"]);
            return Task.FromResult(parser.Command == "scrape");
        }),
        
        new("CommandLineParser.ParseOptions", () =>
        {
            CommandLineParser parser = CommandLineParser.Parse(["cmd", "--verbose", "--timeout=30"]);
            return Task.FromResult(parser.Options["verbose"] == "true" && parser.Options["timeout"] == "30");
        }),
        
        new("CommandLineParser.EmptyArgs", () =>
        {
            CommandLineParser parser = CommandLineParser.Parse([]);
            return Task.FromResult(parser.Command is null && parser.Options.Count == 0);
        }),
        
        new("RetryPolicies.CreatePipeline", () =>
        {
            Polly.ResiliencePipeline pipeline = RetryPolicies.CreatePipeline();
            return Task.FromResult(pipeline is not null);
        }),
        
        new("RetryPolicies.CreateHttpPipeline", () =>
        {
            Polly.ResiliencePipeline<HttpResponseMessage> pipeline = RetryPolicies.CreateHttpPipeline();
            return Task.FromResult(pipeline is not null);
        }),
        
        new("RetryPolicies.GetCombinedPolicy", () =>
        {
            Polly.IAsyncPolicy<HttpResponseMessage> policy = RetryPolicies.GetCombinedPolicy();
            return Task.FromResult(policy is not null);
        }),
        
        new("RetryConfig.Defaults", () =>
        {
            RetryConfig config = new();
            return Task.FromResult(config.MaxRetries == 5 && config.InitialDelayMs == 1000);
        }),
        
        new("RetryConfig.Custom", () =>
        {
            RetryConfig config = new(MaxRetries: 10, InitialDelayMs: 500, BackoffMultiplier: 3.0);
            return Task.FromResult(config.MaxRetries == 10 && config.BackoffMultiplier == 3.0);
        }),
        
        new("CliRunners.AllHaveNames", async () =>
        {
            ICliRunner[] runners = CliComparison.GetAllRunners();
            foreach (ICliRunner runner in runners)
            {
                if (string.IsNullOrEmpty(runner.Name)) return false;
                int result = await runner.RunAsync(["scrape"]);
                if (result != 0) return false;
            }
            return true;
        }),
        
        new("SpectreCliRunner.Scrape", async () =>
        {
            SpectreCliRunner cli = new();
            return await cli.RunAsync(["scrape", "--verbose"]) == 0;
        }),
        
        new("CommandLineParserRunner.Scrape", async () =>
        {
            CommandLineParserRunner cli = new();
            return await cli.RunAsync(["scrape", "--verbose"]) == 0;
        }),
        
        new("MusicBrainz.GetRelease", async () =>
        {
            MusicBrainzService service = new();
            List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "David Bowie", release: "Heroes", maxResults: 1);
            if (results.Count == 0) throw new Exception("No results");
            MusicBrainzRelease? release = await service.GetReleaseAsync(results[0].Id);
            return release is { } r && r.Title.Length > 0;
        }),
        
        new("MusicBrainz.SearchArtists", async () =>
        {
            MusicBrainzService service = new();
            List<MusicBrainzSearchResult> results = await service.SearchArtistsAsync("David Bowie", maxResults: 5);
            return results.Count > 0 && results.Any(r => r.Title?.Contains("Bowie") == true);
        })
    ];
}
