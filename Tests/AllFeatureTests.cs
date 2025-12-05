using TUnit.Core;

namespace Playground.Tests;

public class AllFeatureTests
{
    [Test]
    public async Task MailTm_CreateAccount_Succeeds()
    {
        MailTmService service = new();
        MailTmAccount account = await service.CreateAccountAsync();
        await service.DeleteAccountAsync();
        
        await Assert.That(account.Address).Contains("@");
        await Assert.That(account.Id).IsNotEmpty();
    }

    [Test]
    public async Task MailTm_GetInboxUnauth_Throws()
    {
        MailTmService service = new();
        MailTmException? ex = await Assert.ThrowsAsync<MailTmException>(() => service.GetInboxAsync());
        await Assert.That(ex!.Message).Contains("Not authenticated");
    }

    [Test]
    public async Task MusicBrainz_SearchSpaceOddity_FindsResult()
    {
        MusicMetadataService service = new();
        MusicSearchResult result = await service.SearchAsync("Space Oddity", "David Bowie")
            ?? throw new Exception("Expected result");
        
        await Assert.That(result.Source).IsEqualTo("MusicBrainz");
        await Assert.That(result.Title).Contains("Space Oddity", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task MusicBrainz_SearchHeroes_FindsResult()
    {
        MusicMetadataService service = new();
        MusicSearchResult result = await service.SearchAsync("Heroes", "David Bowie")
            ?? throw new Exception("Expected result");
        
        await Assert.That(result.Title).Contains("Heroes", StringComparison.OrdinalIgnoreCase);
        await Assert.That(result.ExternalId).IsNotEmpty();
    }

    [Test]
    public async Task MusicBrainz_NonexistentTrack_ReturnsNull()
    {
        MusicMetadataService service = new();
        MusicSearchResult? result = await service.SearchAsync("xyznonexistent123456789", "FakeArtist");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Discogs_WithToken_SearchWorks()
    {
        string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
            ?? throw new Exception("DISCOGS_USER_TOKEN not set");
        
        MusicMetadataService service = new(token);
        MusicSearchResult result = await service.SearchAsync("Heroes", "David Bowie")
            ?? throw new Exception("Expected result");
        
        await Assert.That(result.Title).IsNotEmpty();
    }

    [Test]
    public async Task Discogs_SearchAsync_ReturnsResults()
    {
        string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
            ?? throw new Exception("DISCOGS_USER_TOKEN not set");
        
        DiscogsService service = new(new DiscogsClientConfig(token));
        List<DiscogsSearchResult> results = await service.SearchAsync(artist: "David Bowie", maxResults: 5);
        
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Discogs_GetRelease_ReturnsData()
    {
        string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
            ?? throw new Exception("DISCOGS_USER_TOKEN not set");
        
        DiscogsService service = new(new DiscogsClientConfig(token));
        DiscogsRelease result = await service.GetReleaseAsync(249504)
            ?? throw new Exception("Expected release");
        
        await Assert.That(result.Title).IsNotEmpty();
    }

    [Test]
    public async Task Discogs_GetMaster_ReturnsData()
    {
        string token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
            ?? throw new Exception("DISCOGS_USER_TOKEN not set");
        
        DiscogsService service = new(new DiscogsClientConfig(token));
        DiscogsMaster result = await service.GetMasterAsync(23420)
            ?? throw new Exception("Expected master");
        
        await Assert.That(result.Title).IsNotEmpty();
    }

    [Test]
    [Arguments("{\"id\":\"123\"}")]
    [Arguments("data[index]")]
    [Arguments("C:\\Program Files\\[app]")]
    public async Task SpectreLogger_SpecialChars_NoThrow(string message)
    {
        await Assert.That(() =>
        {
            SpectreLogger.Info(message);
            SpectreLogger.Warning(message);
            SpectreLogger.Error(message);
        }).ThrowsNothing();
    }

    [Test]
    public async Task SpectreLogger_Table_NoThrow()
    {
        Dictionary<string, string> data = new()
        {
            ["Key[0]"] = "Value[brackets]",
            ["Normal"] = "Value"
        };
        await Assert.That(() => SpectreLogger.Table("Test", data)).ThrowsNothing();
    }

    [Test]
    [Arguments("scrape", "scrape")]
    [Arguments("help", "help")]
    [Arguments("metadata", "metadata")]
    public async Task CommandLineParser_ParseCommand_Works(string input, string expected)
    {
        CommandLineParser parser = CommandLineParser.Parse([input]);
        await Assert.That(parser.Command).IsEqualTo(expected);
    }

    [Test]
    public async Task CommandLineParser_ParseOptions_Works()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--verbose", "--timeout=30"]);
        await Assert.That(parser.Options["verbose"]).IsEqualTo("true");
        await Assert.That(parser.Options["timeout"]).IsEqualTo("30");
    }

    [Test]
    public async Task CommandLineParser_EmptyArgs_NullCommand()
    {
        CommandLineParser parser = CommandLineParser.Parse([]);
        await Assert.That(parser.Command).IsNull();
        await Assert.That(parser.Options).IsEmpty();
    }

    [Test]
    public async Task RetryPolicies_CreatePipeline_NotNull()
    {
        Polly.ResiliencePipeline pipeline = RetryPolicies.CreatePipeline();
        await Assert.That(pipeline).IsNotNull();
    }

    [Test]
    public async Task RetryPolicies_CreateHttpPipeline_NotNull()
    {
        Polly.ResiliencePipeline<HttpResponseMessage> pipeline = RetryPolicies.CreateHttpPipeline();
        await Assert.That(pipeline).IsNotNull();
    }

    [Test]
    public async Task RetryPolicies_GetCombinedPolicy_NotNull()
    {
        Polly.IAsyncPolicy<HttpResponseMessage> policy = RetryPolicies.GetCombinedPolicy();
        await Assert.That(policy).IsNotNull();
    }

    [Test]
    public async Task RetryConfig_Defaults_Correct()
    {
        RetryConfig config = new();
        await Assert.That(config.MaxRetries).IsEqualTo(5);
        await Assert.That(config.InitialDelayMs).IsEqualTo(1000);
        await Assert.That(config.BackoffMultiplier).IsEqualTo(2.0);
    }

    [Test]
    public async Task RetryConfig_Custom_Correct()
    {
        RetryConfig config = new(MaxRetries: 10, InitialDelayMs: 500, BackoffMultiplier: 3.0);
        await Assert.That(config.MaxRetries).IsEqualTo(10);
        await Assert.That(config.InitialDelayMs).IsEqualTo(500);
        await Assert.That(config.BackoffMultiplier).IsEqualTo(3.0);
    }

    [Test]
    public async Task CliRunners_AllHaveNames()
    {
        ICliRunner[] runners = CliComparison.GetAllRunners();
        foreach (ICliRunner runner in runners)
        {
            await Assert.That(runner.Name).IsNotEmpty();
            int result = await runner.RunAsync(["scrape"]);
            await Assert.That(result).IsEqualTo(0);
        }
    }

    [Test]
    public async Task SpectreCliRunner_Scrape_Works()
    {
        SpectreCliRunner cli = new();
        int result = await cli.RunAsync(["scrape", "--verbose"]);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task CommandLineParserRunner_Scrape_Works()
    {
        CommandLineParserRunner cli = new();
        int result = await cli.RunAsync(["scrape", "--verbose"]);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task MusicBrainz_GetRelease_ReturnsData()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "David Bowie", release: "Heroes", maxResults: 1);
        
        await Assert.That(results).IsNotEmpty();
        
        MusicBrainzRelease release = await service.GetReleaseAsync(results[0].Id)
            ?? throw new Exception("Expected release");
        
        await Assert.That(release.Title).IsNotEmpty();
    }

    [Test]
    public async Task MusicBrainz_SearchArtists_FindsBowie()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchArtistsAsync("David Bowie", maxResults: 5);
        
        await Assert.That(results).IsNotEmpty();
        await Assert.That(results).Contains(r => r.Title?.Contains("Bowie") == true);
    }
}
