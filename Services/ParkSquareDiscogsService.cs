using ParkSquare.Discogs;
using ParkSquare.Discogs.Dto;

namespace Playground.Services;

public class ParkSquareClientConfig(string token) : IClientConfig
{
    public string AuthToken => token;
    public string BaseUrl => "https://api.discogs.com";
}

public record ParkSquareSearchResult(
    int ReleaseId,
    int? MasterId,
    string? Title,
    string? Artist,
    string? Year,
    string? Country,
    string? Format,
    string? Label,
    string? CatalogNumber,
    string? Thumb,
    string? ResourceUrl
);

public record ParkSquareRelease(
    int Id,
    string Title,
    int Year,
    string? Country,
    int MasterId,
    List<string> Artists,
    List<string> Labels,
    List<string> Genres,
    List<string> Styles,
    List<ParkSquareTrack> TracklistItems,
    List<ParkSquareFormat> Formats,
    string? Notes,
    string? Uri
);

public record ParkSquareTrack(string Position, string Title, string Duration, string? Type);

public record ParkSquareFormat(string Name, string Quantity, List<string> Descriptions);

public record ParkSquareMaster(
    int Id,
    string Title,
    int Year,
    int MainReleaseId,
    int MostRecentReleaseId,
    List<string> Artists,
    List<string> Genres,
    List<string> Styles,
    List<ParkSquareTrack> TracklistItems,
    string? Uri
);

public record ParkSquareVersion(
    int Id,
    string Title,
    string? Format,
    string? Label,
    string? Country,
    string? Year,
    string? CatalogNumber,
    string? Thumb,
    string? ResourceUrl
);

public class ParkSquareDiscogsService
{
    readonly DiscogsClient Client;
    readonly string Token;

    public ParkSquareDiscogsService(string token)
    {
        Token =
            token
            ?? throw new ArgumentException("DISCOGS_USER_TOKEN is required for ParkSquare.Discogs");
        HttpClient http = new(new HttpClientHandler());
        Client = new DiscogsClient(http, new ApiQueryBuilder(new ParkSquareClientConfig(token)));
    }

    async Task<T> ExecuteWithLockAsync<T>(Func<Task<T>> action)
    {
        await GlobalRateLimiter.Lock.WaitAsync();
        try
        {
            // Discogs allows 60 requests per minute (1 per second).
            // We add a delay to be safe and avoid 429s.
            await Task.Delay(1500);
            return await action();
        }
        finally
        {
            GlobalRateLimiter.Lock.Release();
        }
    }

    public async Task<List<ParkSquareSearchResult>> SearchAsync(
        SearchCriteria criteria,
        int maxResults = 100
    )
    {
        return await ExecuteWithLockAsync(async () =>
        {
            SpectreLogger.Info(
                $"ParkSquare.Discogs: Searching with criteria - Artist: {criteria.Artist}, Title: {criteria.ReleaseTitle}, Year: {criteria.Year}"
            );

            SearchResults results = await Client.SearchAsync(
                criteria,
                new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
            );

            SpectreLogger.Info($"ParkSquare.Discogs: Found {results.Results.Count} results");

            return results
                .Results.Take(maxResults)
                .Select(r => new ParkSquareSearchResult(
                    ReleaseId: r.ReleaseId,
                    MasterId: r.MasterId,
                    Title: r.Title,
                    Artist: ExtractArtistFromTitle(r.Title),
                    Year: r.Year,
                    Country: r.Country,
                    Format: r.Format is not null ? string.Join(", ", r.Format) : null,
                    Label: r.Label is not null ? string.Join(", ", r.Label) : null,
                    CatalogNumber: r.CatalogNumber,
                    Thumb: r.Thumb,
                    ResourceUrl: r.ResourceUrl
                ))
                .ToList();
        });
    }

    public async Task<List<ParkSquareSearchResult>> SearchAllAsync(SearchCriteria criteria)
    {
        return await ExecuteWithLockAsync(async () =>
        {
            SpectreLogger.Info(
                $"ParkSquare.Discogs: Searching ALL with criteria - Artist: {criteria.Artist}, Title: {criteria.ReleaseTitle}"
            );

            SearchResults results = await Client.SearchAllAsync(criteria);

            SpectreLogger.Info($"ParkSquare.Discogs: Found {results.Results.Count} total results");

            return results
                .Results.Select(r => new ParkSquareSearchResult(
                    ReleaseId: r.ReleaseId,
                    MasterId: r.MasterId,
                    Title: r.Title,
                    Artist: ExtractArtistFromTitle(r.Title),
                    Year: r.Year,
                    Country: r.Country,
                    Format: r.Format is not null ? string.Join(", ", r.Format) : null,
                    Label: r.Label is not null ? string.Join(", ", r.Label) : null,
                    CatalogNumber: r.CatalogNumber,
                    Thumb: r.Thumb,
                    ResourceUrl: r.ResourceUrl
                ))
                .ToList();
        });
    }

    public async Task<ParkSquareRelease?> GetReleaseAsync(int releaseId)
    {
        return await ExecuteWithLockAsync(async () =>
        {
            SpectreLogger.Info($"ParkSquare.Discogs: Getting release {releaseId}");

            Release? release = await Client.GetReleaseAsync(releaseId);
            if (release is null)
                return null;

            return new ParkSquareRelease(
                Id: release.ReleaseId,
                Title: release.Title ?? "",
                Year: release.Year,
                Country: release.Country,
                MasterId: release.MasterId,
                Artists: release
                    .Artists?.Select(a => a.Name ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList() ?? [],
                Labels: release
                    .Labels?.Select(l => l.Name ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList() ?? [],
                Genres: release.Genres?.ToList() ?? [],
                Styles: release.Styles?.ToList() ?? [],
                TracklistItems: release
                    .Tracklist?.Select(t => new ParkSquareTrack(
                        Position: t.Position ?? "",
                        Title: t.Title ?? "",
                        Duration: t.Duration ?? "",
                        Type: t.Type
                    ))
                    .ToList() ?? [],
                Formats: release
                    .Formats?.Select(f => new ParkSquareFormat(
                        Name: f.Name ?? "",
                        Quantity: f.Quantity,
                        Descriptions: f.Descriptions?.ToList() ?? []
                    ))
                    .ToList() ?? [],
                Notes: release.Notes,
                Uri: release.Uri
            );
        });
    }

    public async Task<ParkSquareMaster?> GetMasterAsync(int masterId)
    {
        return await ExecuteWithLockAsync(async () =>
        {
            SpectreLogger.Info($"ParkSquare.Discogs: Getting master {masterId}");

            MasterRelease? master = await Client.GetMasterReleaseAsync(masterId);
            if (master is null)
                return null;

            return new ParkSquareMaster(
                Id: master.MasterId,
                Title: master.Title ?? "",
                Year: master.Year,
                MainReleaseId: master.MainReleaseId,
                MostRecentReleaseId: master.MostRecentReleaseId,
                Artists: master
                    .Artists?.Select(a => a.Name ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList() ?? [],
                Genres: master.Genres?.ToList() ?? [],
                Styles: master.Styles?.ToList() ?? [],
                TracklistItems: master
                    .Tracklist?.Select(t => new ParkSquareTrack(
                        Position: t.Position ?? "",
                        Title: t.Title ?? "",
                        Duration: t.Duration ?? "",
                        Type: t.Type
                    ))
                    .ToList() ?? [],
                Uri: master.Uri
            );
        });
    }

    public async Task<List<ParkSquareVersion>> GetVersionsAsync(int masterId, int maxResults = 100)
    {
        return await ExecuteWithLockAsync(async () =>
        {
            SpectreLogger.Info($"ParkSquare.Discogs: Getting versions for master {masterId}");

            VersionResults results = await Client.GetVersionsAsync(
                new VersionsCriteria(masterId),
                new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
            );

            return results
                .Versions.Take(maxResults)
                .Select(v => new ParkSquareVersion(
                    Id: v.ReleaseId,
                    Title: v.Title ?? "",
                    Format: v.Format,
                    Label: v.Label,
                    Country: v.Country,
                    Year: v.ReleaseYear,
                    CatalogNumber: v.CatalogNumber,
                    Thumb: v.Thumb,
                    ResourceUrl: v.ResourceUrl
                ))
                .ToList();
        });
    }

    public async Task<Dictionary<string, List<ParkSquareTrack>>> GetTracksByMediaAsync(
        int releaseId
    )
    {
        return await ExecuteWithLockAsync(async () =>
        {
            SpectreLogger.Info(
                $"ParkSquare.Discogs: Getting tracks by media for release {releaseId}"
            );

            Release? release = await Client.GetReleaseAsync(releaseId);
            if (release?.Tracklist is null)
                return [];

            Dictionary<string, List<Tracklist>> mediaDict = release.Tracklist.SplitMedia();

            return mediaDict.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                    kvp.Value.Select(t => new ParkSquareTrack(
                            Position: t.Position ?? "",
                            Title: t.Title ?? "",
                            Duration: t.Duration ?? "",
                            Type: t.Type
                        ))
                        .ToList()
            );
        });
    }

    public async Task<ParkSquareSearchResult?> SearchFirstAsync(
        string? artist = null,
        string? releaseTitle = null,
        string? track = null,
        int? year = null
    )
    {
        SearchCriteria criteria = new()
        {
            Artist = artist,
            ReleaseTitle = releaseTitle,
            Track = track,
            Year = year,
        };

        List<ParkSquareSearchResult> results = await SearchAsync(criteria, maxResults: 1);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<(
        ParkSquareRelease? Release,
        ParkSquareMaster? Master
    )> GetReleaseWithMasterAsync(int releaseId)
    {
        ParkSquareRelease? release = await GetReleaseAsync(releaseId);
        if (release is null || release.MasterId == 0)
            return (release, null);

        ParkSquareMaster? master = await GetMasterAsync(release.MasterId);
        return (release, master);
    }

    public async Task<List<ParkSquareRelease>> GetFullVersionsAsync(
        int masterId,
        int maxVersions = 10
    )
    {
        List<ParkSquareVersion> versions = await GetVersionsAsync(masterId, maxVersions);
        List<ParkSquareRelease> releases = [];

        foreach (ParkSquareVersion version in versions)
        {
            ParkSquareRelease? release = await GetReleaseAsync(version.Id);
            if (release is not null)
                releases.Add(release);
        }

        return releases;
    }

    static string? ExtractArtistFromTitle(string? title)
    {
        if (string.IsNullOrEmpty(title))
            return null;
        int dashIndex = title.IndexOf(" - ");
        return dashIndex > 0 ? title[..dashIndex].Trim() : null;
    }
}
