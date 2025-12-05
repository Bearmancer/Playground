namespace Playground.Tests;

[TestClass]
public sealed class SpectreLoggerTests
{
    [TestMethod]
    [DataRow("{\"id\":\"123\"}")]
    [DataRow("data[index]")]
    [DataRow("C:\\Program Files\\[app]")]
    [DataRow("")]
    [DataRow("normal text")]
    [DataRow("unicode: 日本語")]
    public void SpecialChars_NoThrow(string message)
    {
        Should.NotThrow(() =>
        {
            SpectreLogger.Info(message);
            SpectreLogger.Warning(message);
            SpectreLogger.Error(message);
        });
    }

    [TestMethod]
    public void Table_NoThrow()
    {
        Dictionary<string, string> data = new() { ["Key"] = "Value" };
        Should.NotThrow(() => SpectreLogger.Table("Test", data));
    }

    [TestMethod]
    public void Table_Empty_NoThrow()
    {
        Dictionary<string, string> empty = [];
        Should.NotThrow(() => SpectreLogger.Table("Test", empty));
    }

    [TestMethod]
    public void Rule_NoThrow()
    {
        Should.NotThrow(() => SpectreLogger.Rule("Test"));
    }

    [TestMethod]
    public void Success_NoThrow()
    {
        Should.NotThrow(() => SpectreLogger.Success("Done"));
    }
}

[TestClass]
public sealed class CommandLineParserTests
{
    [TestMethod]
    [DataRow("scrape", "scrape")]
    [DataRow("help", "help")]
    [DataRow("UPPER", "UPPER")]
    public void ParseCommand(string input, string expected)
    {
        CommandLineParser parser = CommandLineParser.Parse([input]);
        parser.Command.ShouldBe(expected);
    }

    [TestMethod]
    public void ParseOptions_Equals()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--timeout=30"]);
        parser.Options["timeout"].ShouldBe("30");
    }

    [TestMethod]
    public void ParseOptions_Boolean()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--verbose"]);
        parser.Options["verbose"].ShouldBe("true");
    }

    [TestMethod]
    public void EmptyArgs()
    {
        CommandLineParser parser = CommandLineParser.Parse([]);
        parser.Command.ShouldBeNull();
        parser.Options.Count.ShouldBe(0);
    }

    [TestMethod]
    public void HasOption_True()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--verbose"]);
        parser.HasOption("verbose").ShouldBeTrue();
    }

    [TestMethod]
    public void HasOption_False()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd"]);
        parser.HasOption("verbose").ShouldBeFalse();
    }

    [TestMethod]
    public void GetOption_ReturnsValue()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--timeout=30"]);
        parser.GetOption("timeout").ShouldBe("30");
    }

    [TestMethod]
    public void GetOption_ReturnsNull()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd"]);
        parser.GetOption("timeout").ShouldBeNull();
    }
}

[TestClass]
public sealed class RetryPoliciesTests
{
    [TestMethod]
    public void CreatePipeline_NotNull()
    {
        Polly.ResiliencePipeline pipeline = RetryPolicies.CreatePipeline();
        pipeline.ShouldNotBeNull();
    }

    [TestMethod]
    public void CreateHttpPipeline_NotNull()
    {
        Polly.ResiliencePipeline<HttpResponseMessage> pipeline = RetryPolicies.CreateHttpPipeline();
        pipeline.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetCombinedPolicy_NotNull()
    {
        Polly.IAsyncPolicy<HttpResponseMessage> policy = RetryPolicies.GetCombinedPolicy();
        policy.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Pipeline_Executes()
    {
        Polly.ResiliencePipeline pipeline = RetryPolicies.CreatePipeline();
        int result = 0;
        await pipeline.ExecuteAsync(async _ =>
        {
            result = 42;
            await Task.CompletedTask;
        });
        result.ShouldBe(42);
    }
}

[TestClass]
public sealed class RetryConfigTests
{
    [TestMethod]
    public void Defaults()
    {
        RetryConfig config = new();
        config.MaxRetries.ShouldBe(10);
        config.InitialDelayMs.ShouldBe(3000);
        config.BackoffMultiplier.ShouldBe(2.0);
    }

    [TestMethod]
    public void Custom()
    {
        RetryConfig config = new(MaxRetries: 5, InitialDelayMs: 500, BackoffMultiplier: 3.0);
        config.MaxRetries.ShouldBe(5);
        config.InitialDelayMs.ShouldBe(500);
        config.BackoffMultiplier.ShouldBe(3.0);
    }
}

[TestClass]
public sealed class RecordTests
{
    [TestMethod]
    public void DiscogsSearchResult_Properties()
    {
        DiscogsSearchResult r = new(
            1,
            2,
            "Title",
            "Artist",
            2024,
            "US",
            "CD",
            "Label",
            "CAT",
            "thumb"
        );
        r.ReleaseId.ShouldBe(1);
        r.MasterId.ShouldBe(2);
        r.Title.ShouldBe("Title");
    }

    [TestMethod]
    public void DiscogsRelease_Properties()
    {
        DiscogsRelease r = new(
            1,
            "Title",
            2024,
            "US",
            2,
            ["Artist"],
            ["Label"],
            ["Rock"],
            ["Pop"],
            [],
            [],
            [],
            "Notes"
        );
        r.Id.ShouldBe(1);
        r.Title.ShouldBe("Title");
        r.Artists.ShouldContain("Artist");
    }

    [TestMethod]
    public void DiscogsMaster_Properties()
    {
        DiscogsMaster m = new(1, "Title", 2024, 100, 200, ["Artist"], ["Rock"], ["Pop"], []);
        m.Id.ShouldBe(1);
        m.MainReleaseId.ShouldBe(100);
    }

    [TestMethod]
    public void DiscogsTrack_Properties()
    {
        DiscogsTrack t = new("A1", "Track", "3:45");
        t.Position.ShouldBe("A1");
        t.Title.ShouldBe("Track");
    }

    [TestMethod]
    public void MusicBrainzSearchResult_Properties()
    {
        MusicBrainzSearchResult r = new(
            Guid.NewGuid(),
            "Title",
            "Artist",
            2024,
            "US",
            "Official",
            "disc"
        );
        r.Title.ShouldBe("Title");
        r.Artist.ShouldBe("Artist");
    }

    [TestMethod]
    public void MusicBrainzRelease_Properties()
    {
        MusicBrainzRelease r = new(
            Guid.NewGuid(),
            "Title",
            "Artist",
            "Artist1, Artist2",
            DateOnly.FromDateTime(DateTime.Now),
            "US",
            "Official",
            "123",
            "disc",
            [],
            []
        );
        r.Title.ShouldBe("Title");
        r.Status.ShouldBe("Official");
    }

    [TestMethod]
    public void MusicSearchResult_Properties()
    {
        MusicSearchResult r = new("Title", "Artist", 2024, "MusicBrainz", "123");
        r.Title.ShouldBe("Title");
        r.Source.ShouldBe("MusicBrainz");
    }
}
