namespace Playground.Services;

using Playground.Utilities;

public record DiscogsSearchResult(
    int ReleaseId,
    int? MasterId,
    string? Title,
    string? Artist,
    int? Year,
    string? Country,
    string? Format,
    string? Label,
    string? CatalogNumber,
    string? Thumb
);

public record DiscogsRelease(
    int Id,
    string Title,
    int Year,
    string? Country,
    int? MasterId,
    List<string> Artists,
    List<string> Labels,
    List<string> Genres,
    List<string> Styles,
    List<DiscogsTrack> Tracks,
    List<DiscogsFormat> Formats,
    List<DiscogsCredit> Credits,
    string? Notes
);

public record DiscogsTrack(string Position, string Title, string Duration);

public record DiscogsFormat(string Name, string? Quantity, List<string> Descriptions);

public record DiscogsCredit(string Name, string Role, string? Tracks);

public record DiscogsMaster(
    int Id,
    string Title,
    int Year,
    int MainReleaseId,
    int MostRecentReleaseId,
    List<string> Artists,
    List<string> Genres,
    List<string> Styles,
    List<DiscogsTrack> Tracks
);

public record DiscogsVersion(
    int Id,
    string Title,
    string? Format,
    string? Label,
    string? Country,
    int? Year,
    string? CatalogNumber
);

sealed class DiscogsClientConfig(string token) : IClientConfig
{
    public string AuthToken => token;
    public string BaseUrl => "https://api.discogs.com";
}

public sealed class DiscogsService
{
    internal DiscogsClient Client { get; }

    public DiscogsService(string? token)
    {
        string validToken =
            token ?? throw new ArgumentException("Discogs token is required", nameof(token));
        HttpClient http = new(new HttpClientHandler());
        Client = new DiscogsClient(http, new ApiQueryBuilder(new DiscogsClientConfig(validToken)));
    }

    public async Task<List<DiscogsSearchResult>> SearchAsync(
        string? artist = null,
        string? release = null,
        string? track = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        int maxResults = 50
    )
    {
        SearchCriteria criteria = new()
        {
            Artist = artist,
            ReleaseTitle = release,
            Track = track,
            Year = year,
            Label = label,
            Genre = genre,
        };

        return await ExecuteSafeListAsync(async () =>
        {
            SearchResults results = await Client.SearchAsync(
                criteria,
                new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
            );

            return results
                .Results.Take(maxResults)
                .Select(r => new DiscogsSearchResult(
                    ReleaseId: r.ReleaseId,
                    MasterId: r.MasterId,
                    Title: r.Title,
                    Artist: ExtractArtist(r.Title),
                    Year: ParseYear(r.Year),
                    Country: r.Country,
                    Format: r.Format is { } fmt ? string.Join(", ", fmt) : null,
                    Label: r.Label is { } lbl ? string.Join(", ", lbl) : null,
                    CatalogNumber: r.CatalogNumber,
                    Thumb: r.Thumb
                ))
                .ToList();
        });
    }

    public async Task<DiscogsSearchResult?> SearchFirstAsync(
        string? artist = null,
        string? release = null,
        string? track = null,
        int? year = null,
        string? label = null,
        string? genre = null
    )
    {
        List<DiscogsSearchResult> results = await SearchAsync(
            artist,
            release,
            track,
            year,
            label,
            genre,
            1
        );
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<DiscogsRelease?> GetReleaseAsync(int releaseId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            Release? release = await Client.GetReleaseAsync(releaseId);
            if (release is null)
                return null;

            return new DiscogsRelease(
                Id: release.ReleaseId,
                Title: release.Title ?? "",
                Year: release.Year,
                Country: release.Country,
                MasterId: release.MasterId,
                Artists: release
                    .Artists?.Select(a => a.Name ?? "")
                    .Where(n => n.Length > 0)
                    .ToList() ?? [],
                Labels: release.Labels?.Select(l => l.Name ?? "").Where(n => n.Length > 0).ToList()
                    ?? [],
                Genres: release.Genres?.ToList() ?? [],
                Styles: release.Styles?.ToList() ?? [],
                Tracks: release
                    .Tracklist?.Select(t => new DiscogsTrack(
                        Position: t.Position ?? "",
                        Title: t.Title ?? "",
                        Duration: t.Duration ?? ""
                    ))
                    .ToList() ?? [],
                Formats: release
                    .Formats?.Select(f => new DiscogsFormat(
                        Name: f.Name ?? "",
                        Quantity: f.Quantity,
                        Descriptions: f.Descriptions?.ToList() ?? []
                    ))
                    .ToList() ?? [],
                Credits: release
                    .ExtraArtists?.Select(a => new DiscogsCredit(
                        Name: a.Name ?? "",
                        Role: a.Role ?? "",
                        Tracks: a.Tracks
                    ))
                    .Where(c => c.Name.Length > 0)
                    .ToList() ?? [],
                Notes: release.Notes
            );
        });
    }

    public async Task<DiscogsMaster?> GetMasterAsync(int masterId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            MasterRelease? master = await Client.GetMasterReleaseAsync(masterId);
            if (master is null)
                return null;

            return new DiscogsMaster(
                Id: master.MasterId,
                Title: master.Title ?? "",
                Year: master.Year,
                MainReleaseId: master.MainReleaseId,
                MostRecentReleaseId: master.MostRecentReleaseId,
                Artists: master.Artists?.Select(a => a.Name ?? "").Where(n => n.Length > 0).ToList()
                    ?? [],
                Genres: master.Genres?.ToList() ?? [],
                Styles: master.Styles?.ToList() ?? [],
                Tracks: master
                    .Tracklist?.Select(t => new DiscogsTrack(
                        Position: t.Position ?? "",
                        Title: t.Title ?? "",
                        Duration: t.Duration ?? ""
                    ))
                    .ToList() ?? []
            );
        });
    }

    public async Task<List<DiscogsVersion>> GetVersionsAsync(int masterId, int maxResults = 50)
    {
        return await ExecuteSafeListAsync(async () =>
        {
            VersionResults results = await Client.GetVersionsAsync(
                new VersionsCriteria(masterId),
                new PageOptions { PageNumber = 1, PageSize = Math.Min(maxResults, 100) }
            );

            return results
                .Versions.Take(maxResults)
                .Select(v => new DiscogsVersion(
                    Id: v.ReleaseId,
                    Title: v.Title ?? "",
                    Format: v.Format,
                    Label: v.Label,
                    Country: v.Country,
                    Year: ParseYear(v.ReleaseYear),
                    CatalogNumber: v.CatalogNumber
                ))
                .ToList();
        });
    }

    public async Task<Dictionary<string, List<DiscogsTrack>>> GetTracksByMediaAsync(int releaseId)
    {
        return await ExecuteSafeDictAsync(async () =>
        {
            Release? release = await Client.GetReleaseAsync(releaseId);
            if (release?.Tracklist is null)
                return [];

            Dictionary<string, List<Tracklist>> mediaDict = release.Tracklist.SplitMedia();

            return mediaDict.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                    kvp.Value.Select(t => new DiscogsTrack(
                            Position: t.Position ?? "",
                            Title: t.Title ?? "",
                            Duration: t.Duration ?? ""
                        ))
                        .ToList()
            );
        });
    }

    static Task<T> ExecuteAsync<T>(Func<Task<T>> action) =>
        Resilience.ExecuteAsync(action, Resilience.DiscogsThrottle, "Discogs");

    // Safe wrapper that returns default value instead of throwing
    static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action)
        where T : class => ExecuteAsync(action);

    // Safe wrapper for lists - returns empty instead of throwing
    static Task<List<T>> ExecuteSafeListAsync<T>(Func<Task<List<T>>> action) =>
        ExecuteAsync(action);

    // Safe wrapper for dictionaries - returns empty instead of throwing
    static Task<Dictionary<TKey, TValue>> ExecuteSafeDictAsync<TKey, TValue>(
        Func<Task<Dictionary<TKey, TValue>>> action
    )
        where TKey : notnull => ExecuteAsync(action);

    static string? ExtractArtist(string? title) =>
        title?.Contains(" - ") == true ? title.Split(" - ")[0].Trim() : null;

    static int? ParseYear(string? year) => int.TryParse(year, out int y) ? y : null;
}
