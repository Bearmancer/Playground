using TUnit.Core;
using ParkSquare.Discogs;

namespace Playground.Tests;

public class MailTmServiceTests
{
    [Test]
    public async Task MailTm_EndToEnd_Flow()
    {
        // Combine multiple tests to avoid Rate Limiting (429 Too Many Requests)
        // caused by parallel execution of multiple account creation tests.
        
        MailTmService service = new();
        
        // 1. Create Account
        MailTmAccount account = await service.CreateAccountAsync();
        
        try
        {
            // 2. Validate Account Properties
            await Assert.That(account.Address).Contains("@");
            await Assert.That(account.Address).Contains(".");
            await Assert.That(account.Address.Split('@').Length).IsEqualTo(2);
            await Assert.That(account.Id).IsNotEmpty();
            await Assert.That(account.Quota).IsGreaterThan(0);
            await Assert.That(account.CreatedAt).IsGreaterThan(DateTime.MinValue);
            await Assert.That(account.IsDisabled).IsFalse();

            // 3. Get Inbox (Authenticated)
            List<MailTmMessage> inbox = await service.GetInboxAsync();
            await Assert.That(inbox).IsNotNull();
            await Assert.That(inbox).IsEmpty();
        }
        finally
        {
            // 4. Delete Account
            await service.DeleteAccountAsync();
        }
    }

    [Test]
    public async Task GetInboxUnauth_ThrowsMailTmException()
    {
        MailTmService service = new();
        MailTmException? ex = await Assert.ThrowsAsync<MailTmException>(() => service.GetInboxAsync());
        await Assert.That(ex!.Message).Contains("Not authenticated");
    }

    [Test]
    public async Task MultipleAccounts_EachUnique()
    {
        // Add significant delay to avoid 429
        await Task.Delay(5000);

        MailTmService service1 = new();
        MailTmService service2 = new();
        
        MailTmAccount account1 = await service1.CreateAccountAsync();
        try
        {
            await Task.Delay(5000); 
            MailTmAccount account2 = await service2.CreateAccountAsync();
            try
            {
                await Assert.That(account1.Address).IsNotEqualTo(account2.Address);
            }
            finally
            {
                await service2.DeleteAccountAsync();
            }
        }
        finally
        {
            await service1.DeleteAccountAsync();
        }
    }
}

public class MusicBrainzServiceTests
{
    [Test]
    public async Task SearchReleases_DavidBowie_ReturnsResults()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "David Bowie", maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task SearchReleases_WithYear_FiltersCorrectly()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "David Bowie", release: "Heroes", year: 1977, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task SearchReleases_NonexistentArtist_ReturnsEmpty()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "xyznonexistent123456789abc", maxResults: 5);
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task SearchReleases_NullArtist_ReturnsEmpty()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: null, release: null, maxResults: 5);
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task SearchReleases_EmptyStrings_ReturnsEmpty()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "", release: "", maxResults: 5);
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task SearchReleases_MaxResults1_ReturnsOne()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "David Bowie", maxResults: 1);
        await Assert.That(results.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SearchReleases_SpecialCharacters_Handles()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleasesAsync(artist: "Björk", maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_ValidId_ReturnsRelease()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> searchResults = await service.SearchReleasesAsync(artist: "David Bowie", release: "Heroes", maxResults: 1);
        await Assert.That(searchResults).IsNotEmpty();
        
        MusicBrainzRelease? release = await service.GetReleaseAsync(searchResults[0].Id);
        await Assert.That(release).IsNotNull();
        await Assert.That(release!.Title).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_WithTracks_ReturnsTracks()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> searchResults = await service.SearchReleasesAsync(artist: "David Bowie", release: "Heroes", maxResults: 1);
        MusicBrainzRelease? release = await service.GetReleaseAsync(searchResults[0].Id, includeTracks: true);
        await Assert.That(release?.Tracks).IsNotNull();
    }

    [Test]
    public async Task SearchArtists_DavidBowie_FindsBowie()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchArtistsAsync("David Bowie", maxResults: 5);
        await Assert.That(results).IsNotEmpty();
        await Assert.That(results.Any(r => r.Title?.Contains("Bowie") == true)).IsTrue();
    }

    [Test]
    public async Task SearchArtists_NonexistentArtist_ReturnsEmpty()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchArtistsAsync("xyznonexistent123456789abc", maxResults: 5);
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task SearchReleaseGroups_ReturnsResults()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> results = await service.SearchReleaseGroupsAsync(artist: "David Bowie", releaseGroup: "Heroes", maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task GetReleaseGroup_ValidId_ReturnsData()
    {
        MusicBrainzService service = new();
        List<MusicBrainzSearchResult> searchResults = await service.SearchReleaseGroupsAsync(artist: "David Bowie", releaseGroup: "Heroes", maxResults: 1);
        await Assert.That(searchResults).IsNotEmpty();
        
        MusicBrainzReleaseGroup? rg = await service.GetReleaseGroupAsync(searchResults[0].Id);
        await Assert.That(rg).IsNotNull();
        await Assert.That(rg!.Title).IsNotEmpty();
    }

    [Test]
    public async Task Constructor_CustomParams_Accepted()
    {
        MusicBrainzService service = new("TestApp", "2.0", "test@test.com");
        List<MusicBrainzSearchResult> results = await service.SearchArtistsAsync("Beatles", maxResults: 1);
        await Assert.That(results).IsNotEmpty();
    }
}

public class DiscogsServiceTests
{
    static string GetToken() =>
        Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
            ?? throw new Exception("DISCOGS_USER_TOKEN not set");

    [Test]
    public async Task Search_ByArtist_ReturnsResults()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsSearchResult> results = await service.SearchAsync(artist: "David Bowie", maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByRelease_ReturnsResults()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsSearchResult> results = await service.SearchAsync(release: "Heroes", maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByTrack_ReturnsResults()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsSearchResult> results = await service.SearchAsync(track: "Heroes", maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByYear_ReturnsResults()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsSearchResult> results = await service.SearchAsync(artist: "David Bowie", year: 1977, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_NonexistentArtist_ReturnsEmpty()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsSearchResult> results = await service.SearchAsync(artist: "xyznonexistent123456789abc", maxResults: 5);
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task Search_MaxResults1_ReturnsOne()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsSearchResult> results = await service.SearchAsync(artist: "David Bowie", maxResults: 1);
        await Assert.That(results.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetRelease_ValidId_ReturnsRelease()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release).IsNotNull();
        await Assert.That(release!.Title).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_InvalidId_ReturnsNull()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsRelease? release = await service.GetReleaseAsync(999999999);
        await Assert.That(release).IsNull();
    }

    [Test]
    public async Task GetRelease_HasArtists()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release?.Artists).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_HasTracklist()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release?.Tracklist).IsNotEmpty();
    }

    [Test]
    public async Task GetMaster_ValidId_ReturnsMaster()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master).IsNotNull();
        await Assert.That(master!.Title).IsNotEmpty();
    }

    [Test]
    public async Task GetMaster_InvalidId_ReturnsNull()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsMaster? master = await service.GetMasterAsync(999999999);
        await Assert.That(master).IsNull();
    }

    [Test]
    public async Task GetMaster_HasTracklist()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master?.Tracklist).IsNotEmpty();
    }

    [Test]
    public async Task GetVersions_ValidMasterId_ReturnsVersions()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsVersion> versions = await service.GetVersionsAsync(23420, maxResults: 5);
        await Assert.That(versions).IsNotEmpty();
    }

    [Test]
    public async Task GetVersions_InvalidMasterId_ReturnsEmpty()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        List<DiscogsVersion> versions = await service.GetVersionsAsync(-1, maxResults: 5);
        await Assert.That(versions).IsEmpty();
    }

    [Test]
    public async Task GetArtistReleasesFirst_ReturnsRelease()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsRelease? release = await service.GetArtistReleasesFirstAsync("David Bowie");
        await Assert.That(release).IsNotNull();
    }

    [Test]
    public async Task GetArtistReleasesFirst_InvalidArtist_ReturnsNull()
    {
        DiscogsService service = new(new DiscogsClientConfig(GetToken()));
        DiscogsRelease? release = await service.GetArtistReleasesFirstAsync("xyznonexistent123456789abc");
        await Assert.That(release).IsNull();
    }

    [Test]
    public async Task Config_NullToken_Accepted()
    {
        DiscogsClientConfig config = new(null);
        await Assert.That(config.BaseUrl).IsEqualTo("https://api.discogs.com");
    }

    [Test]
    public async Task Config_WithToken_Accepted()
    {
        DiscogsClientConfig config = new("test-token");
        await Assert.That(config.AuthToken).IsEqualTo("test-token");
    }
}

public class ParkSquareDiscogsServiceTests
{
    static string GetToken() =>
        Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN")
            ?? throw new Exception("DISCOGS_USER_TOKEN not set");

    [Test]
    public async Task Constructor_NullToken_Throws()
    {
        Exception? ex = null;
        try { ParkSquareDiscogsService _ = new(null!); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Constructor_ValidToken_NoThrow()
    {
        Exception? ex = null;
        try { ParkSquareDiscogsService _ = new(GetToken()); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task Search_ByArtist_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "David Bowie" }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByReleaseTitle_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { ReleaseTitle = "Heroes" }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByArtistAndTitle_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "David Bowie", ReleaseTitle = "Heroes" }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByYear_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "David Bowie", Year = 1977 }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByTrack_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Track = "Heroes" }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByBarcode_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Barcode = "724352190201" }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_NonexistentArtist_ReturnsEmpty()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "xyznonexistent123456789abc" }, maxResults: 5);
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task Search_MaxResults1_ReturnsOne()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "David Bowie" }, maxResults: 1);
        await Assert.That(results.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Search_MaxResults100_ReturnsMax100()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "Beatles" }, maxResults: 100);
        await Assert.That(results.Count).IsLessThanOrEqualTo(100);
    }

    [Test]
    public async Task SearchFirst_ReturnsFirstResult()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareSearchResult? result = await service.SearchFirstAsync(artist: "David Bowie", releaseTitle: "Heroes");
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task SearchFirst_NonexistentArtist_ReturnsNull()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareSearchResult? result = await service.SearchFirstAsync(artist: "xyznonexistent123456789abc");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetRelease_ValidId_ReturnsRelease()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release).IsNotNull();
        await Assert.That(release!.Title).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_HasId()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release!.Id).IsEqualTo(249504);
    }

    [Test]
    public async Task GetRelease_HasArtists()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release?.Artists).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_HasLabels()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release?.Labels).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_HasGenres()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release?.Genres).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_HasTracklist()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release?.TracklistItems).IsNotEmpty();
    }

    [Test]
    public async Task GetRelease_HasFormats()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareRelease? release = await service.GetReleaseAsync(249504);
        await Assert.That(release?.Formats).IsNotEmpty();
    }

    [Test]
    public async Task GetMaster_ValidId_ReturnsMaster()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master).IsNotNull();
        await Assert.That(master!.Title).IsNotEmpty();
    }

    [Test]
    public async Task GetMaster_HasId()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master!.Id).IsEqualTo(23420);
    }

    [Test]
    public async Task GetMaster_HasMainReleaseId()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master!.MainReleaseId).IsGreaterThan(0);
    }

    [Test]
    public async Task GetMaster_HasMostRecentReleaseId()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master!.MostRecentReleaseId).IsGreaterThan(0);
    }

    [Test]
    public async Task GetMaster_HasArtists()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master?.Artists).IsNotEmpty();
    }

    [Test]
    public async Task GetMaster_HasTracklist()
    {
        ParkSquareDiscogsService service = new(GetToken());
        ParkSquareMaster? master = await service.GetMasterAsync(23420);
        await Assert.That(master?.TracklistItems).IsNotEmpty();
    }

    [Test]
    public async Task GetVersions_ValidMasterId_ReturnsVersions()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareVersion> versions = await service.GetVersionsAsync(23420, maxResults: 5);
        await Assert.That(versions).IsNotEmpty();
    }

    [Test]
    public async Task GetVersions_MaxResults5_ReturnsMax5()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareVersion> versions = await service.GetVersionsAsync(23420, maxResults: 5);
        await Assert.That(versions.Count).IsLessThanOrEqualTo(5);
    }

    [Test]
    public async Task GetVersions_EachHasId()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareVersion> versions = await service.GetVersionsAsync(23420, maxResults: 5);
        await Assert.That(versions.All(v => v.Id > 0)).IsTrue();
    }

    [Test]
    public async Task GetVersions_EachHasTitle()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareVersion> versions = await service.GetVersionsAsync(23420, maxResults: 5);
        await Assert.That(versions.All(v => !string.IsNullOrEmpty(v.Title))).IsTrue();
    }

    [Test]
    public async Task GetTracksByMedia_ReturnsMediaDict()
    {
        ParkSquareDiscogsService service = new(GetToken());
        Dictionary<string, List<ParkSquareTrack>> media = await service.GetTracksByMediaAsync(249504);
        await Assert.That(media).IsNotEmpty();
    }

    [Test]
    public async Task GetTracksByMedia_EachMediaHasTracks()
    {
        ParkSquareDiscogsService service = new(GetToken());
        Dictionary<string, List<ParkSquareTrack>> media = await service.GetTracksByMediaAsync(249504);
        await Assert.That(media.Values.All(tracks => tracks.Count > 0)).IsTrue();
    }

    [Test]
    public async Task GetReleaseWithMaster_ReturnsBoth()
    {
        ParkSquareDiscogsService service = new(GetToken());
        (ParkSquareRelease? release, ParkSquareMaster? master) = await service.GetReleaseWithMasterAsync(249504);
        await Assert.That(release).IsNotNull();
        await Assert.That(master).IsNotNull();
    }

    [Test]
    public async Task GetFullVersions_ReturnsFullReleases()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareRelease> releases = await service.GetFullVersionsAsync(23420, maxVersions: 3);
        await Assert.That(releases).IsNotEmpty();
        await Assert.That(releases.All(r => r.Id > 0)).IsTrue();
    }

    [Test]
    public async Task Search_ByCountry_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "David Bowie", Country = "UK" }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }

    [Test]
    public async Task Search_ByFormat_ReturnsResults()
    {
        ParkSquareDiscogsService service = new(GetToken());
        List<ParkSquareSearchResult> results = await service.SearchAsync(new SearchCriteria { Artist = "David Bowie", Format = "Vinyl" }, maxResults: 5);
        await Assert.That(results).IsNotEmpty();
    }
}

public class MusicMetadataServiceTests
{
    [Test]
    public async Task Search_MusicBrainzOnly_Works()
    {
        MusicMetadataService service = new();
        MusicSearchResult? result = await service.SearchAsync("Heroes", "David Bowie");
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Source).IsEqualTo("MusicBrainz");
    }

    [Test]
    public async Task Search_WithDiscogsToken_Works()
    {
        string? token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        if (token is null) throw new Exception("DISCOGS_USER_TOKEN not set");
        
        MusicMetadataService service = new(token);
        MusicSearchResult? result = await service.SearchAsync("Heroes", "David Bowie");
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Search_NonexistentTrack_ReturnsNull()
    {
        MusicMetadataService service = new();
        MusicSearchResult? result = await service.SearchAsync("xyznonexistent123456789", "FakeArtist");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Search_NullArtist_Works()
    {
        MusicMetadataService service = new();
        MusicSearchResult? result = await service.SearchAsync("Heroes", null);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task SearchBoth_ReturnsBothResults()
    {
        string? token = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        if (token is null) throw new Exception("DISCOGS_USER_TOKEN not set");
        
        MusicMetadataService service = new(token);
        (List<DiscogsSearchResult> discogs, List<MusicBrainzSearchResult> mb) = await service.SearchBothAsync(artist: "David Bowie", release: "Heroes");
        
        await Assert.That(mb).IsNotEmpty();
    }

    [Test]
    public async Task MusicBrainz_Accessible()
    {
        MusicMetadataService service = new();
        await Assert.That(service.MusicBrainz).IsNotNull();
    }

    [Test]
    public async Task Discogs_Accessible()
    {
        MusicMetadataService service = new();
        await Assert.That(service.Discogs).IsNotNull();
    }
}

public class SpectreLoggerTests
{
    [Test]
    [Arguments("{\"id\":\"123\"}")]
    [Arguments("data[index]")]
    [Arguments("C:\\Program Files\\[app]")]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("normal text")]
    [Arguments("unicode: 日本語")]
    [Arguments("emoji: 🎵🎸")]
    public async Task SpecialChars_NoThrow(string message)
    {
        Exception? ex = null;
        try
        {
            SpectreLogger.Info(message);
            SpectreLogger.Warning(message);
            SpectreLogger.Error(message);
        }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task Table_NoThrow()
    {
        Dictionary<string, string> data = new()
        {
            ["Key[0]"] = "Value[brackets]",
            ["Normal"] = "Value"
        };
        Exception? ex = null;
        try { SpectreLogger.Table("Test", data); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task Table_EmptyDict_NoThrow()
    {
        Dictionary<string, string> data = [];
        Exception? ex = null;
        try { SpectreLogger.Table("Test", data); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task Rule_NoThrow()
    {
        Exception? ex = null;
        try { SpectreLogger.Rule("Test Rule"); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task Success_NoThrow()
    {
        Exception? ex = null;
        try { SpectreLogger.Success("Success message"); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task KeyValue_NoThrow()
    {
        Exception? ex = null;
        try { SpectreLogger.KeyValue("Key", "Value"); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task Starting_NoThrow()
    {
        Exception? ex = null;
        try { SpectreLogger.Starting("Starting operation"); }
        catch (Exception e) { ex = e; }
        await Assert.That(ex).IsNull();
    }
}

public class CommandLineParserTests
{
    [Test]
    [Arguments("scrape", "scrape")]
    [Arguments("help", "help")]
    [Arguments("metadata", "metadata")]
    [Arguments("UPPERCASE", "UPPERCASE")]
    [Arguments("Mixed-Case", "Mixed-Case")]
    public async Task ParseCommand_Works(string input, string expected)
    {
        CommandLineParser parser = CommandLineParser.Parse([input]);
        await Assert.That(parser.Command).IsEqualTo(expected);
    }

    [Test]
    public async Task ParseOptions_WithEquals()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--timeout=30"]);
        await Assert.That(parser.Options["timeout"]).IsEqualTo("30");
    }

    [Test]
    public async Task ParseOptions_Boolean()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--verbose"]);
        await Assert.That(parser.Options["verbose"]).IsEqualTo("true");
    }

    [Test]
    public async Task ParseOptions_Multiple()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--verbose", "--timeout=30", "--format=json"]);
        await Assert.That(parser.Options["verbose"]).IsEqualTo("true");
        await Assert.That(parser.Options["timeout"]).IsEqualTo("30");
        await Assert.That(parser.Options["format"]).IsEqualTo("json");
    }

    [Test]
    public async Task EmptyArgs_NullCommand()
    {
        CommandLineParser parser = CommandLineParser.Parse([]);
        await Assert.That(parser.Command).IsNull();
        await Assert.That(parser.Options.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Arguments_Captured()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "arg1", "arg2"]);
        await Assert.That(parser.Arguments).Contains("arg1");
        await Assert.That(parser.Arguments).Contains("arg2");
    }

    [Test]
    public async Task HasOption_True()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--verbose"]);
        await Assert.That(parser.HasOption("verbose")).IsTrue();
    }

    [Test]
    public async Task HasOption_False()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd"]);
        await Assert.That(parser.HasOption("verbose")).IsFalse();
    }

    [Test]
    public async Task GetOption_ReturnsValue()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd", "--timeout=30"]);
        await Assert.That(parser.GetOption("timeout")).IsEqualTo("30");
    }

    [Test]
    public async Task GetOption_ReturnsNull()
    {
        CommandLineParser parser = CommandLineParser.Parse(["cmd"]);
        await Assert.That(parser.GetOption("timeout")).IsNull();
    }
}

public class RetryPoliciesTests
{
    [Test]
    public async Task CreatePipeline_NotNull()
    {
        Polly.ResiliencePipeline pipeline = RetryPolicies.CreatePipeline();
        await Assert.That(pipeline).IsNotNull();
    }

    [Test]
    public async Task CreateHttpPipeline_NotNull()
    {
        Polly.ResiliencePipeline<HttpResponseMessage> pipeline = RetryPolicies.CreateHttpPipeline();
        await Assert.That(pipeline).IsNotNull();
    }

    [Test]
    public async Task GetCombinedPolicy_NotNull()
    {
        Polly.IAsyncPolicy<HttpResponseMessage> policy = RetryPolicies.GetCombinedPolicy();
        await Assert.That(policy).IsNotNull();
    }

    [Test]
    public async Task CreatePipeline_ExecutesSuccessfully()
    {
        Polly.ResiliencePipeline pipeline = RetryPolicies.CreatePipeline();
        int result = 0;
        await pipeline.ExecuteAsync(async _ => { result = 42; await Task.CompletedTask; });
        await Assert.That(result).IsEqualTo(42);
    }
}

public class RetryConfigTests
{
    [Test]
    public async Task Defaults_Correct()
    {
        RetryConfig config = new();
        await Assert.That(config.MaxRetries).IsEqualTo(5);
        await Assert.That(config.InitialDelayMs).IsEqualTo(1000);
        await Assert.That(config.BackoffMultiplier).IsEqualTo(2.0);
    }

    [Test]
    public async Task Custom_Correct()
    {
        RetryConfig config = new(MaxRetries: 10, InitialDelayMs: 500, BackoffMultiplier: 3.0);
        await Assert.That(config.MaxRetries).IsEqualTo(10);
        await Assert.That(config.InitialDelayMs).IsEqualTo(500);
        await Assert.That(config.BackoffMultiplier).IsEqualTo(3.0);
    }

    [Test]
    public async Task ZeroRetries_Accepted()
    {
        RetryConfig config = new(MaxRetries: 0, InitialDelayMs: 1000, BackoffMultiplier: 2.0);
        await Assert.That(config.MaxRetries).IsEqualTo(0);
    }

    [Test]
    public async Task ZeroDelay_Accepted()
    {
        RetryConfig config = new(MaxRetries: 5, InitialDelayMs: 0, BackoffMultiplier: 2.0);
        await Assert.That(config.InitialDelayMs).IsEqualTo(0);
    }

    [Test]
    public async Task OneMultiplier_Accepted()
    {
        RetryConfig config = new(MaxRetries: 5, InitialDelayMs: 1000, BackoffMultiplier: 1.0);
        await Assert.That(config.BackoffMultiplier).IsEqualTo(1.0);
    }
}

public class CliRunnersTests
{
    [Test]
    public async Task AllHaveNames()
    {
        ICliRunner[] runners = CliComparison.GetAllRunners();
        foreach (ICliRunner runner in runners)
        {
            await Assert.That(runner.Name).IsNotEmpty();
        }
    }

    [Test]
    public async Task AllRunScrape()
    {
        ICliRunner[] runners = CliComparison.GetAllRunners();
        foreach (ICliRunner runner in runners)
        {
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
    public async Task SpectreCliRunner_Search_Works()
    {
        SpectreCliRunner cli = new();
        int result = await cli.RunAsync(["search", "Bowie"]);
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
    public async Task CoconaRunner_Scrape_Works()
    {
        CoconaRunner cli = new();
        int result = await cli.RunAsync(["scrape"]);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task SystemCommandLineRunner_Scrape_Works()
    {
        SystemCommandLineRunner cli = new();
        int result = await cli.RunAsync(["scrape"]);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task GetAllRunners_ReturnsNonEmpty()
    {
        ICliRunner[] runners = CliComparison.GetAllRunners();
        await Assert.That(runners.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task GetAllRunners_AllUniqueNames()
    {
        ICliRunner[] runners = CliComparison.GetAllRunners();
        HashSet<string> names = runners.Select(r => r.Name).ToHashSet();
        await Assert.That(names.Count).IsEqualTo(runners.Length);
    }
}

public class RecordTypesTests
{
    [Test]
    public async Task DiscogsSearchResult_AllProperties()
    {
        DiscogsSearchResult result = new(
            Id: 1, Title: "Test", Artist: "Artist", Year: 2024,
            Country: "US", Format: "CD", Label: "Label",
            CatalogNumber: "CAT001", ResourceUrl: "url",
            MasterId: "123", MasterUrl: "murl", Thumb: "thumb"
        );
        await Assert.That(result.Id).IsEqualTo(1);
        await Assert.That(result.Title).IsEqualTo("Test");
        await Assert.That(result.Artist).IsEqualTo("Artist");
    }

    [Test]
    public async Task DiscogsRelease_AllProperties()
    {
        DiscogsRelease release = new(
            Id: 1, Title: "Test", Year: 2024, Country: "US",
            Artists: ["Artist"], Labels: ["Label"],
            Formats: ["CD"], Genres: ["Rock"], Styles: ["Pop Rock"],
            Tracklist: [], Notes: "Notes", MasterId: 123,
            MasterUrl: "url", ResourceUrl: "rurl", Uri: "uri",
            ReleasedFormatted: null
        );
        await Assert.That(release.Id).IsEqualTo(1);
        await Assert.That(release.Title).IsEqualTo("Test");
    }

    [Test]
    public async Task MusicBrainzSearchResult_AllProperties()
    {
        MusicBrainzSearchResult result = new(
            Id: Guid.NewGuid(), Title: "Test", Artist: "Artist",
            Year: 2024, Country: "US", Status: "Official",
            Disambiguation: "disc"
        );
        await Assert.That(result.Title).IsEqualTo("Test");
        await Assert.That(result.Artist).IsEqualTo("Artist");
    }

    [Test]
    public async Task MusicSearchResult_AllProperties()
    {
        MusicSearchResult result = new(
            Title: "Test", Artist: "Artist", Year: 2024,
            Source: "MusicBrainz", ExternalId: "123"
        );
        await Assert.That(result.Title).IsEqualTo("Test");
        await Assert.That(result.Source).IsEqualTo("MusicBrainz");
    }

    [Test]
    public async Task ParkSquareSearchResult_AllProperties()
    {
        ParkSquareSearchResult result = new(
            ReleaseId: 1, MasterId: 2, Title: "Test", Artist: "Artist",
            Year: "2024", Country: "US", Format: "CD", Label: "Label",
            CatalogNumber: "CAT001", Thumb: "thumb", ResourceUrl: "url"
        );
        await Assert.That(result.ReleaseId).IsEqualTo(1);
        await Assert.That(result.MasterId).IsEqualTo(2);
    }

    [Test]
    public async Task ParkSquareRelease_AllProperties()
    {
        ParkSquareRelease release = new(
            Id: 1, Title: "Test", Year: 2024, Country: "US", MasterId: 2,
            Artists: ["Artist"], Labels: ["Label"], Genres: ["Rock"],
            Styles: ["Pop Rock"], TracklistItems: [], Formats: [], Notes: "Notes", Uri: "uri"
        );
        await Assert.That(release.Id).IsEqualTo(1);
        await Assert.That(release.MasterId).IsEqualTo(2);
    }

    [Test]
    public async Task ParkSquareMaster_AllProperties()
    {
        ParkSquareMaster master = new(
            Id: 1, Title: "Test", Year: 2024, MainReleaseId: 100,
            MostRecentReleaseId: 200,
            Artists: ["Artist"], Genres: ["Rock"], Styles: ["Pop Rock"],
            TracklistItems: [], Uri: "uri"
        );
        await Assert.That(master.Id).IsEqualTo(1);
        await Assert.That(master.MainReleaseId).IsEqualTo(100);
    }

    [Test]
    public async Task ParkSquareTrack_AllProperties()
    {
        ParkSquareTrack track = new(
            Position: "A1", Title: "Track Title", Duration: "3:45", Type: "track"
        );
        await Assert.That(track.Position).IsEqualTo("A1");
        await Assert.That(track.Title).IsEqualTo("Track Title");
    }

    [Test]
    public async Task ParkSquareFormat_AllProperties()
    {
        ParkSquareFormat format = new(
            Name: "Vinyl", Quantity: "1", Descriptions: ["LP", "Album"]
        );
        await Assert.That(format.Name).IsEqualTo("Vinyl");
        await Assert.That(format.Quantity).IsEqualTo("1");
    }

    [Test]
    public async Task ParkSquareVersion_AllProperties()
    {
        ParkSquareVersion version = new(
            Id: 1, Title: "Test", Format: "CD", Label: "Label",
            Country: "US", Year: "2024", CatalogNumber: "CAT001",
            Thumb: "thumb", ResourceUrl: "url"
        );
        await Assert.That(version.Id).IsEqualTo(1);
        await Assert.That(version.Title).IsEqualTo("Test");
    }
}
