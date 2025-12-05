namespace Playground.Services;

using Playground.Utilities;

public record MusicBrainzSearchResult(
    Guid Id,
    string Title,
    string? Artist,
    int? Year,
    string? Country,
    string? Status,
    string? Disambiguation
);

public record MusicBrainzRelease(
    Guid Id,
    string Title,
    string? Artist,
    string? ArtistCredit,
    DateOnly? Date,
    string? Country,
    string? Status,
    string? Barcode,
    string? Disambiguation,
    List<MusicBrainzTrack> Tracks,
    List<MusicBrainzCredit> Credits
);

public record MusicBrainzTrack(
    Guid Id,
    string Title,
    int Position,
    TimeSpan? Length,
    Guid? RecordingId
);

public record MusicBrainzCredit(string Name, string Role, Guid? ArtistId);

public record MusicBrainzReleaseGroup(
    Guid Id,
    string Title,
    string? Artist,
    string? PrimaryType,
    List<string>? SecondaryTypes,
    DateOnly? FirstReleaseDate,
    int ReleaseCount,
    string? Disambiguation
);

public record MusicBrainzArtist(
    Guid Id,
    string Name,
    string? Type,
    string? Country,
    string? Disambiguation,
    DateOnly? BeginDate,
    DateOnly? EndDate
);

public sealed class MusicBrainzService(
    string appName = "PlaygroundApp",
    string appVersion = "1.0",
    string contact = "user@example.com"
)
{
    internal Query Query { get; } = new(appName, appVersion, contact);

    public async Task<List<MusicBrainzSearchResult>> SearchReleasesAsync(
        string? artist = null,
        string? release = null,
        int? year = null,
        string? label = null,
        string? genre = null,
        int maxResults = 25
    )
    {
        string query = BuildQuery(artist, release, year, label, genre);
        if (string.IsNullOrEmpty(query))
            return [];

        return await ExecuteSafeListAsync(async () =>
        {
            ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(
                query,
                maxResults
            );
            return results
                .Results.Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Title ?? "",
                    Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                    Year: r.Item.Date?.Year,
                    Country: r.Item.Country,
                    Status: r.Item.Status,
                    Disambiguation: r.Item.Disambiguation
                ))
                .ToList();
        });
    }

    public async Task<MusicBrainzSearchResult?> SearchFirstReleaseAsync(
        string? artist = null,
        string? release = null,
        int? year = null,
        string? label = null,
        string? genre = null
    )
    {
        List<MusicBrainzSearchResult> results = await SearchReleasesAsync(
            artist,
            release,
            year,
            label,
            genre,
            1
        );
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<MusicBrainzRelease?> GetReleaseAsync(
        Guid releaseId,
        bool includeTracks = true,
        bool includeCredits = false
    )
    {
        return await ExecuteSafeAsync(async () =>
        {
            Include inc = Include.ArtistCredits;
            if (includeTracks)
                inc |= Include.Recordings | Include.Media;
            if (includeCredits)
                inc |= Include.ArtistRelationships;

            IRelease? release = await Query.LookupReleaseAsync(releaseId, inc);
            if (release is null)
                return null;

            List<MusicBrainzTrack> tracks = [];
            if (release.Media is { } media)
            {
                foreach (IMedium medium in media)
                {
                    if (medium.Tracks is not { } mediumTracks)
                        continue;

                    foreach (ITrack track in mediumTracks)
                    {
                        tracks.Add(
                            new MusicBrainzTrack(
                                Id: track.Id,
                                Title: track.Title ?? track.Recording?.Title ?? "",
                                Position: track.Position ?? 0,
                                Length: track.Length,
                                RecordingId: track.Recording?.Id
                            )
                        );
                    }
                }
            }

            List<MusicBrainzCredit> credits = [];
            if (release.Relationships is { } relationships)
            {
                foreach (IRelationship rel in relationships)
                {
                    if (rel.Artist is { } artist && !string.IsNullOrEmpty(rel.Type))
                    {
                        credits.Add(
                            new MusicBrainzCredit(
                                Name: artist.Name ?? "",
                                Role: rel.Type,
                                ArtistId: artist.Id
                            )
                        );
                    }
                }
            }

            return new MusicBrainzRelease(
                Id: release.Id,
                Title: release.Title ?? "",
                Artist: release.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                ArtistCredit: release.ArtistCredit is { } artistCredits
                    ? string.Join(", ", artistCredits.Select(a => a.Artist?.Name ?? ""))
                    : null,
                Date: release.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
                Country: release.Country,
                Status: release.Status,
                Barcode: release.Barcode,
                Disambiguation: release.Disambiguation,
                Tracks: tracks,
                Credits: credits
            );
        });
    }

    public async Task<List<MusicBrainzSearchResult>> SearchReleaseGroupsAsync(
        string? artist = null,
        string? releaseGroup = null,
        int maxResults = 25
    )
    {
        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(artist))
            parts.Add($"artist:\"{artist}\"");
        if (!string.IsNullOrWhiteSpace(releaseGroup))
            parts.Add($"releasegroup:\"{releaseGroup}\"");

        if (parts.Count == 0)
            return [];

        string query = string.Join(" AND ", parts);

        return await ExecuteSafeListAsync(async () =>
        {
            ISearchResults<ISearchResult<IReleaseGroup>> results =
                await Query.FindReleaseGroupsAsync(query, maxResults);
            return results
                .Results.Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Title ?? "",
                    Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                    Year: r.Item.FirstReleaseDate?.Year,
                    Country: null,
                    Status: r.Item.PrimaryType,
                    Disambiguation: r.Item.Disambiguation
                ))
                .ToList();
        });
    }

    public async Task<MusicBrainzReleaseGroup?> GetReleaseGroupAsync(Guid releaseGroupId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            IReleaseGroup? rg = await Query.LookupReleaseGroupAsync(
                releaseGroupId,
                Include.ArtistCredits | Include.Releases
            );
            if (rg is null)
                return null;

            return new MusicBrainzReleaseGroup(
                Id: rg.Id,
                Title: rg.Title ?? "",
                Artist: rg.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                PrimaryType: rg.PrimaryType,
                SecondaryTypes: rg.SecondaryTypes?.ToList(),
                FirstReleaseDate: rg.FirstReleaseDate?.NearestDate is DateTime dt
                    ? DateOnly.FromDateTime(dt)
                    : null,
                ReleaseCount: rg.Releases?.Count ?? 0,
                Disambiguation: rg.Disambiguation
            );
        });
    }

    public async Task<List<MusicBrainzSearchResult>> SearchArtistsAsync(
        string artist,
        int maxResults = 25
    )
    {
        return await ExecuteSafeListAsync(async () =>
        {
            ISearchResults<ISearchResult<IArtist>> results = await Query.FindArtistsAsync(
                $"artist:\"{artist}\"",
                maxResults
            );
            return results
                .Results.Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Name ?? "",
                    Artist: r.Item.Name,
                    Year: r.Item.LifeSpan?.Begin?.Year,
                    Country: r.Item.Country,
                    Status: r.Item.Type,
                    Disambiguation: r.Item.Disambiguation
                ))
                .ToList();
        });
    }

    public async Task<MusicBrainzArtist?> GetArtistAsync(Guid artistId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            IArtist? artist = await Query.LookupArtistAsync(artistId);
            if (artist is null)
                return null;

            return new MusicBrainzArtist(
                Id: artist.Id,
                Name: artist.Name ?? "",
                Type: artist.Type,
                Country: artist.Country,
                Disambiguation: artist.Disambiguation,
                BeginDate: artist.LifeSpan?.Begin?.NearestDate is DateTime b
                    ? DateOnly.FromDateTime(b)
                    : null,
                EndDate: artist.LifeSpan?.End?.NearestDate is DateTime e
                    ? DateOnly.FromDateTime(e)
                    : null
            );
        });
    }

    static Task<T> ExecuteAsync<T>(Func<Task<T>> action) =>
        Resilience.ExecuteAsync(action, Resilience.MusicBrainzThrottle, "MusicBrainz");

    static Task<T?> ExecuteSafeAsync<T>(Func<Task<T?>> action)
        where T : class => ExecuteAsync(action);

    static Task<List<T>> ExecuteSafeListAsync<T>(Func<Task<List<T>>> action) =>
        ExecuteAsync(action);

    static string BuildQuery(
        string? artist,
        string? release,
        int? year,
        string? label,
        string? genre
    )
    {
        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(artist))
            parts.Add($"artist:\"{artist}\"");
        if (!string.IsNullOrWhiteSpace(release))
            parts.Add($"release:\"{release}\"");
        if (!string.IsNullOrWhiteSpace(label))
            parts.Add($"label:\"{label}\"");
        if (!string.IsNullOrWhiteSpace(genre))
            parts.Add($"tag:\"{genre}\"");
        if (year.HasValue)
            parts.Add($"date:{year}");
        return string.Join(" AND ", parts);
    }
}
