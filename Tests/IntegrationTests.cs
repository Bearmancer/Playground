namespace Playground.Tests;

[TestClass]
public sealed class MailTmTests
{
    [TestMethod]
    public async Task EndToEnd()
    {
        MailTmService service = new();
        MailTmAccount account = await service.CreateAccountAsync();

        try
        {
            account.Address.ShouldContain("@");
            account.Id.ShouldNotBeNullOrEmpty();
            account.Quota.ShouldBeGreaterThan(0);

            List<MailTmMessage> inbox = await service.GetInboxAsync();
            inbox.ShouldBeEmpty();
        }
        finally
        {
            await service.DeleteAccountAsync();
        }
    }

    [TestMethod]
    public async Task GetInboxUnauth_Throws()
    {
        MailTmService service = new();
        MailTmException ex = await Should.ThrowAsync<MailTmException>(() =>
            service.GetInboxAsync()
        );
        ex.Message.ShouldContain("Not authenticated");
    }
}

[TestClass]
public sealed class MusicBrainzServiceTests
{
    [TestMethod]
    public async Task SearchReleases_ReturnsResults()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(
            artist: "David Bowie",
            maxResults: 5
        );
        results.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task SearchReleases_WithYear()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(
            artist: "David Bowie",
            release: "Heroes",
            year: 1977,
            maxResults: 5
        );
        results.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task SearchReleases_Nonexistent_Empty()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(
            artist: "xyznonexistent123456789abc",
            maxResults: 5
        );
        results.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task SearchReleases_Empty_Empty()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(
            artist: null,
            release: null,
            maxResults: 5
        );
        results.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task SearchFirstRelease_ReturnsOne()
    {
        MusicBrainzService service = new();
        MusicBrainzSearchResult? result = await service.SearchFirstReleaseAsync(
            artist: "David Bowie",
            release: "Heroes"
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetRelease_Valid()
    {
        MusicBrainzService service = new();
        MusicBrainzSearchResult? search = await service.SearchFirstReleaseAsync(
            artist: "David Bowie",
            release: "Heroes"
        );
        search.ShouldNotBeNull();

        MusicBrainzRelease? release = await service.GetReleaseAsync(search!.Id);
        release.ShouldNotBeNull();
        release!.Title.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task GetRelease_WithTracks()
    {
        MusicBrainzService service = new();
        MusicBrainzSearchResult? search = await service.SearchFirstReleaseAsync(
            artist: "David Bowie",
            release: "Heroes"
        );
        MusicBrainzRelease? release = await service.GetReleaseAsync(
            search!.Id,
            includeTracks: true
        );
        release?.Tracks.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task SearchArtists_FindsBowie()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchArtistsAsync(
            "David Bowie",
            maxResults: 5
        );
        results.ShouldNotBeEmpty();
        results.Any(r => r.Title.Contains("Bowie")).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SearchReleaseGroups_ReturnsResults()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleaseGroupsAsync(
            artist: "David Bowie",
            releaseGroup: "Heroes",
            maxResults: 5
        );
        results.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task GetReleaseGroup_Valid()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> search = await service.SearchReleaseGroupsAsync(
            artist: "David Bowie",
            releaseGroup: "Heroes",
            maxResults: 1
        );
        search.ShouldNotBeEmpty();

        MusicBrainzReleaseGroup? rg = await service.GetReleaseGroupAsync(search[0].Id);
        rg.ShouldNotBeNull();
        rg!.Title.ShouldNotBeNullOrEmpty();
    }
}

[TestClass]
public sealed class DiscogsServiceTests
{
    static string GetToken() =>
        Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
        ?? throw new Exception("DISCOGS_USER_TOKEN not set");

    [TestMethod]
    public void Constructor_NullToken_Throws()
    {
        Should.Throw<ArgumentException>(() => new DiscogsService(null));
    }

    [TestMethod]
    public async Task Search_ByArtist()
    {
        DiscogsService service = new(GetToken());
        List<DiscogsSearchResult> results = await service.SearchAsync(
            artist: "David Bowie",
            maxResults: 5
        );
        results.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task Search_ByRelease()
    {
        DiscogsService service = new(GetToken());
        List<DiscogsSearchResult> results = await service.SearchAsync(
            release: "Heroes",
            maxResults: 5
        );
        results.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task Search_ByYear()
    {
        DiscogsService service = new(GetToken());
        List<DiscogsSearchResult> results = await service.SearchAsync(
            artist: "David Bowie",
            year: 1977,
            maxResults: 5
        );
        results.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task Search_Nonexistent_Empty()
    {
        DiscogsService service = new(GetToken());
        List<DiscogsSearchResult> results = await service.SearchAsync(
            artist: "xyznonexistent123456789abc",
            maxResults: 5
        );
        results.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task SearchFirst_ReturnsOne()
    {
        DiscogsService service = new(GetToken());
        DiscogsSearchResult? result = await service.SearchFirstAsync(
            artist: "David Bowie",
            release: "Heroes"
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetRelease_Valid()
    {
        DiscogsService service = new(GetToken());
        DiscogsRelease? release = await service.GetReleaseAsync(249504);
        release.ShouldNotBeNull();
        release!.Title.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task GetRelease_HasArtists()
    {
        DiscogsService service = new(GetToken());
        DiscogsRelease? release = await service.GetReleaseAsync(249504);
        release?.Artists.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task GetRelease_HasTracks()
    {
        DiscogsService service = new(GetToken());
        DiscogsRelease? release = await service.GetReleaseAsync(249504);
        release?.Tracks.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task GetMaster_Valid()
    {
        DiscogsService service = new(GetToken());
        DiscogsMaster? master = await service.GetMasterAsync(23420);
        master.ShouldNotBeNull();
        master!.Title.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task GetMaster_HasMainReleaseId()
    {
        DiscogsService service = new(GetToken());
        DiscogsMaster? master = await service.GetMasterAsync(23420);
        master!.MainReleaseId.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public async Task GetVersions_ReturnsVersions()
    {
        DiscogsService service = new(GetToken());
        List<DiscogsVersion> versions = await service.GetVersionsAsync(23420, maxResults: 5);
        versions.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task GetTracksByMedia_ReturnsDict()
    {
        DiscogsService service = new(GetToken());
        Dictionary<string, List<DiscogsTrack>> media = await service.GetTracksByMediaAsync(249504);
        media.ShouldNotBeEmpty();
    }
}

[TestClass]
public sealed class MusicMetadataServiceTests
{
    static string GetToken() =>
        Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
        ?? throw new Exception("DISCOGS_USER_TOKEN not set");

    [TestMethod]
    public async Task Search_MusicBrainzOnly()
    {
        MusicMetadataService service = new();
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "David Bowie", Album: "Heroes")
        );
        result.ShouldNotBeNull();
        result!.Source.ShouldBe("MusicBrainz");
    }

    [TestMethod]
    public async Task Search_WithDiscogs()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "David Bowie", Album: "Heroes")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_Nonexistent_Null()
    {
        MusicMetadataService service = new();
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "FakeArtist", Album: "xyznonexistent123456789")
        );
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task SearchBoth_ReturnsBoth()
    {
        MusicMetadataService service = new(GetToken());
        (List<DiscogsSearchResult>? discogs, List<MusicBrainzSearchResult> mb) =
            await service.SearchBothAsync(new(Artist: "David Bowie", Album: "Heroes"));

        mb.ShouldNotBeEmpty();
        discogs.ShouldNotBeNull();
    }

    [TestMethod]
    public void MusicBrainz_NotNull()
    {
        MusicMetadataService service = new();
        service.MusicBrainz.ShouldNotBeNull();
    }

    [TestMethod]
    public void Discogs_NullWithoutToken()
    {
        MusicMetadataService service = new();
        service.Discogs.ShouldBeNull();
    }

    [TestMethod]
    public void Discogs_NotNullWithToken()
    {
        MusicMetadataService service = new(GetToken());
        service.Discogs.ShouldNotBeNull();
    }

    // Classical Music Tests (Task 11)

    [TestMethod]
    public async Task Classical_Karajan_Dvorak_EMI()
    {
        MusicMetadataService service = new(GetToken());
        UnifiedRelease? release = await service.GetReleaseAsync(
            new(Artist: "Karajan", Album: "Symphony No. 9", Label: "EMI")
        );
        release.ShouldNotBeNull();
        release!.Title.ShouldNotBeNullOrEmpty();
        release.Tracks.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task Classical_Rozhdestvensky_Schnittke()
    {
        MusicMetadataService service = new(GetToken());
        UnifiedRelease? release = await service.GetReleaseAsync(
            new(Artist: "Rozhdestvensky", Album: "Concerto Grosso No. 1")
        );
        release.ShouldNotBeNull();
        release!.Title.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Classical_Barenboim_Symphonies_2014()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "Barenboim", Album: "Symphonies", Year: 2014)
        );
        result.ShouldNotBeNull();
        result!.Title.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Track_87AndCry()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(new(Track: "'87 and Cry"));
        result.ShouldNotBeNull();
        result!.Title.ShouldNotBeNullOrEmpty();
    }

    // Search Permutation Tests

    [TestMethod]
    public async Task Search_ArtistOnly()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(new(Artist: "Pink Floyd"));
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_AlbumOnly()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Album: "The Dark Side of the Moon")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_TrackOnly()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(new(Track: "Money"));
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_ArtistAndAlbum()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "Pink Floyd", Album: "The Dark Side of the Moon")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_ArtistAndTrack()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "Pink Floyd", Track: "Money")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_AlbumAndTrack()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Album: "The Dark Side of the Moon", Track: "Money")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_ArtistAlbumTrack()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "Pink Floyd", Album: "The Dark Side of the Moon", Track: "Money")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_WithLabel()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "David Bowie", Label: "RCA")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_WithGenre()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "David Bowie", Genre: "Rock")
        );
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Search_WithYear()
    {
        MusicMetadataService service = new(GetToken());
        MusicSearchResult? result = await service.SearchAsync(
            new(Artist: "David Bowie", Year: 1977)
        );
        result.ShouldNotBeNull();
    }

    // Sorting Tests

    [TestMethod]
    public async Task Sort_ByReleaseYear()
    {
        MusicMetadataService service = new(GetToken());
        (List<DiscogsSearchResult>? discogs, List<MusicBrainzSearchResult> mb) =
            await service.SearchBothAsync(new(Artist: "David Bowie", SortBy: "releaseyear"));

        mb.Count.ShouldBeGreaterThan(1);
        // Verify descending order
        for (int i = 1; i < mb.Count; i++)
        {
            (mb[i - 1].Year ?? 0).ShouldBeGreaterThanOrEqualTo(mb[i].Year ?? 0);
        }
    }

    [TestMethod]
    public async Task Sort_ByArtist()
    {
        MusicMetadataService service = new(GetToken());
        (List<DiscogsSearchResult>? discogs, List<MusicBrainzSearchResult> _) =
            await service.SearchBothAsync(new(Album: "Greatest Hits", SortBy: "artist"));

        discogs.ShouldNotBeNull();
        discogs!.Count.ShouldBeGreaterThan(1);
    }

    [TestMethod]
    public async Task Sort_TracksByDuration()
    {
        MusicMetadataService service = new(GetToken());
        UnifiedRelease? release = await service.GetReleaseAsync(
            new(Artist: "Pink Floyd", Album: "The Dark Side of the Moon", SortBy: "duration")
        );

        release.ShouldNotBeNull();
        release!.Tracks.Count.ShouldBeGreaterThan(1);
        // Verify descending duration order
        for (int i = 1; i < release.Tracks.Count; i++)
        {
            TimeSpan prevDur = release.Tracks[i - 1].Duration ?? TimeSpan.Zero;
            TimeSpan currDur = release.Tracks[i].Duration ?? TimeSpan.Zero;
            prevDur.ShouldBeGreaterThanOrEqualTo(currDur);
        }
    }

    [TestMethod]
    public async Task Sort_TracksByTitle()
    {
        MusicMetadataService service = new(GetToken());
        UnifiedRelease? release = await service.GetReleaseAsync(
            new(Artist: "Pink Floyd", Album: "The Dark Side of the Moon", SortBy: "track")
        );

        release.ShouldNotBeNull();
        release!.Tracks.Count.ShouldBeGreaterThan(1);
    }

    // Spectre Table Tests

    [TestMethod]
    public async Task GetReleaseTable_ReturnsTable()
    {
        MusicMetadataService service = new(GetToken());
        Spectre.Console.Table? table = await service.GetReleaseTableAsync(
            new(Artist: "David Bowie", Album: "Heroes")
        );
        table.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetReleaseTable_Null_WhenNotFound()
    {
        MusicMetadataService service = new();
        Spectre.Console.Table? table = await service.GetReleaseTableAsync(
            new(Artist: "xyznonexistent123456789", Album: "FakeAlbum")
        );
        table.ShouldBeNull();
    }

    // Credits Tests

    [TestMethod]
    public async Task GetCredits_ReturnsCredits()
    {
        MusicMetadataService service = new(GetToken());
        List<UnifiedCredit> credits = await service.GetCreditsAsync(
            new(Artist: "David Bowie", Album: "Heroes")
        );
        credits.ShouldNotBeNull();
    }

    // UnifiedRelease Fields Tests

    [TestMethod]
    public async Task UnifiedRelease_HasExternalId()
    {
        MusicMetadataService service = new(GetToken());
        UnifiedRelease? release = await service.GetReleaseAsync(
            new(Artist: "David Bowie", Album: "Heroes")
        );
        release.ShouldNotBeNull();
        release!.ExternalId.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task UnifiedRelease_HasSource()
    {
        MusicMetadataService service = new(GetToken());
        UnifiedRelease? release = await service.GetReleaseAsync(
            new(Artist: "David Bowie", Album: "Heroes")
        );
        release.ShouldNotBeNull();
        release!.Source.ShouldBeOneOf("MusicBrainz", "Discogs");
    }

    [TestMethod]
    public async Task UnifiedRelease_TracksHaveRecordingId()
    {
        MusicMetadataService service = new();
        UnifiedRelease? release = await service.GetReleaseAsync(
            new(Artist: "David Bowie", Album: "Heroes")
        );
        release.ShouldNotBeNull();
        // MusicBrainz tracks should have RecordingId
        if (release!.Source == "MusicBrainz" && release.Tracks.Count > 0)
        {
            release.Tracks[0].RecordingId.ShouldNotBeNullOrEmpty();
        }
    }
}

[TestClass]
public sealed class CliRunnerTests
{
    [TestMethod]
    public void AllHaveNames()
    {
        ICliRunner[] runners = CliComparison.GetAllRunners();
        foreach (ICliRunner runner in runners)
        {
            runner.Name.ShouldNotBeNullOrEmpty();
        }
    }

    [TestMethod]
    public async Task AllRunScrape()
    {
        ICliRunner[] runners = CliComparison.GetAllRunners();
        foreach (ICliRunner runner in runners)
        {
            int result = await runner.RunAsync(["scrape"]);
            result.ShouldBe(0);
        }
    }

    [TestMethod]
    public async Task SpectreCliRunner_Scrape()
    {
        SpectreCliRunner cli = new();
        int result = await cli.RunAsync(["scrape", "--verbose"]);
        result.ShouldBe(0);
    }

    [TestMethod]
    public async Task CommandLineParserRunner_Scrape()
    {
        CommandLineParserRunner cli = new();
        int result = await cli.RunAsync(["scrape", "--verbose"]);
        result.ShouldBe(0);
    }

    [TestMethod]
    public async Task CoconaRunner_Scrape()
    {
        CoconaRunner cli = new();
        int result = await cli.RunAsync(["scrape"]);
        result.ShouldBe(0);
    }

    [TestMethod]
    public async Task SystemCommandLineRunner_Scrape()
    {
        SystemCommandLineRunner cli = new();
        int result = await cli.RunAsync(["scrape"]);
        result.ShouldBe(0);
    }
}
