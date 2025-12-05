namespace Playground.Services;

public record DiscogsClientConfig(string? AuthToken = null)
{
    public string BaseUrl { get; } = "https://api.discogs.com";
}

public record DiscogsSearchResult(
    int Id,
    string? Title,
    string? Artist,
    int? Year,
    string? Country,
    string? Format,
    string? Label,
    string? CatalogNumber,
    string? ResourceUrl,
    string? MasterId,
    string? MasterUrl,
    string? Thumb
);

public record DiscogsTrack(
    string Position,
    string Title,
    string Duration,
    string? ExtraArtists
);

public record DiscogsRelease(
    int Id,
    string Title,
    int Year,
    string Country,
    List<string> Artists,
    List<string> Labels,
    List<string> Formats,
    List<string> Genres,
    List<string> Styles,
    List<DiscogsTrack> Tracklist,
    string? Notes,
    int? MasterId,
    string? MasterUrl,
    string? ResourceUrl,
    string? Uri,
    DateTime? ReleasedFormatted
);

public record DiscogsMaster(
    int Id,
    string Title,
    int Year,
    List<string> Artists,
    List<string> Genres,
    List<string> Styles,
    int MainReleaseId,
    string? MainReleaseUrl,
    int VersionsCount,
    string? ResourceUrl,
    string? Uri,
    List<DiscogsTrack> Tracklist
);

public record DiscogsVersion(
    int Id,
    string Title,
    string? Format,
    string? Label,
    string? Country,
    int? Year,
    string? CatalogNumber,
    string? ResourceUrl,
    string? Thumb
);

public record MusicBrainzSearchResult(
    Guid Id,
    string? Title,
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
    List<MusicBrainzTrack> Tracks
);

public record MusicBrainzTrack(
    Guid Id,
    string Title,
    int Position,
    TimeSpan? Length,
    Guid? RecordingId
);

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

public static class GlobalRateLimiter
{
    public static readonly SemaphoreSlim Lock = new(1, 1);
}

public class DiscogsService(DiscogsClientConfig Config)
{
    readonly HttpClient Client = CreateClient(Config);
    readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    static HttpClient CreateClient(DiscogsClientConfig config)
    {
        HttpClient client = new() { BaseAddress = new Uri(config.BaseUrl) };
        client.DefaultRequestHeaders.Add("User-Agent", "PlaygroundApp/1.0");
        if (!string.IsNullOrEmpty(config.AuthToken))
            client.DefaultRequestHeaders.Add("Authorization", $"Discogs token={config.AuthToken}");
        return client;
    }

    public async Task<List<DiscogsSearchResult>> SearchAsync(string? artist = null, string? track = null, string? release = null, int? year = null, int maxResults = 50)
    {
        List<string> queryParts = [];
        if (!string.IsNullOrWhiteSpace(artist)) queryParts.Add($"artist={Uri.EscapeDataString(artist)}");
        if (!string.IsNullOrWhiteSpace(track)) queryParts.Add($"track={Uri.EscapeDataString(track)}");
        if (!string.IsNullOrWhiteSpace(release)) queryParts.Add($"release_title={Uri.EscapeDataString(release)}");
        if (year.HasValue) queryParts.Add($"year={year}");
        queryParts.Add($"per_page={Math.Min(maxResults, 100)}");

        string query = string.Join("&", queryParts);
        string url = $"/database/search?{query}";

        using HttpResponseMessage response = await ExecuteWithRetryAsync(() => Client.GetAsync(url));
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement results = doc.RootElement.GetProperty("results");

        List<DiscogsSearchResult> items = [];
        foreach (JsonElement item in results.EnumerateArray())
        {
            items.Add(new DiscogsSearchResult(
                Id: item.GetProperty("id").GetInt32(),
                Title: item.TryGetProperty("title", out JsonElement t) ? t.GetString() : null,
                Artist: ExtractArtist(item),
                Year: item.TryGetProperty("year", out JsonElement y) ? ParseNullableInt(y) : null,
                Country: item.TryGetProperty("country", out JsonElement c) ? c.GetString() : null,
                Format: item.TryGetProperty("format", out JsonElement f) ? string.Join(", ", f.EnumerateArray().Select(x => x.GetString())) : null,
                Label: item.TryGetProperty("label", out JsonElement l) ? string.Join(", ", l.EnumerateArray().Select(x => x.GetString())) : null,
                CatalogNumber: item.TryGetProperty("catno", out JsonElement cat) ? cat.GetString() : null,
                ResourceUrl: item.TryGetProperty("resource_url", out JsonElement r) ? r.GetString() : null,
                MasterId: item.TryGetProperty("master_id", out JsonElement m) ? m.ToString() : null,
                MasterUrl: item.TryGetProperty("master_url", out JsonElement mu) ? mu.GetString() : null,
                Thumb: item.TryGetProperty("thumb", out JsonElement th) ? th.GetString() : null
            ));
            if (items.Count >= maxResults) break;
        }
        return items;
    }

    public async Task<DiscogsRelease?> GetReleaseAsync(int releaseId)
    {
        string url = $"/releases/{releaseId}";
        using HttpResponseMessage response = await ExecuteWithRetryAsync(() => Client.GetAsync(url));
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        return new DiscogsRelease(
            Id: root.GetProperty("id").GetInt32(),
            Title: root.GetProperty("title").GetString() ?? "",
            Year: root.TryGetProperty("year", out JsonElement y) ? y.GetInt32() : 0,
            Country: root.TryGetProperty("country", out JsonElement c) ? c.GetString() ?? "" : "",
            Artists: ExtractArtistsList(root),
            Labels: ExtractStringArray(root, "labels", "name"),
            Formats: ExtractFormats(root),
            Genres: ExtractStringArraySimple(root, "genres"),
            Styles: ExtractStringArraySimple(root, "styles"),
            Tracklist: ExtractTracklist(root),
            Notes: root.TryGetProperty("notes", out JsonElement n) ? n.GetString() : null,
            MasterId: root.TryGetProperty("master_id", out JsonElement m) ? m.GetInt32() : null,
            MasterUrl: root.TryGetProperty("master_url", out JsonElement mu) ? mu.GetString() : null,
            ResourceUrl: root.TryGetProperty("resource_url", out JsonElement r) ? r.GetString() : null,
            Uri: root.TryGetProperty("uri", out JsonElement u) ? u.GetString() : null,
            ReleasedFormatted: null
        );
    }

    public async Task<DiscogsMaster?> GetMasterAsync(int masterId)
    {
        string url = $"/masters/{masterId}";
        using HttpResponseMessage response = await ExecuteWithRetryAsync(() => Client.GetAsync(url));
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        return new DiscogsMaster(
            Id: root.GetProperty("id").GetInt32(),
            Title: root.GetProperty("title").GetString() ?? "",
            Year: root.TryGetProperty("year", out JsonElement y) ? y.GetInt32() : 0,
            Artists: ExtractArtistsList(root),
            Genres: ExtractStringArraySimple(root, "genres"),
            Styles: ExtractStringArraySimple(root, "styles"),
            MainReleaseId: root.TryGetProperty("main_release", out JsonElement mr) ? mr.GetInt32() : 0,
            MainReleaseUrl: root.TryGetProperty("main_release_url", out JsonElement mru) ? mru.GetString() : null,
            VersionsCount: root.TryGetProperty("versions_count", out JsonElement vc) ? vc.GetInt32() : 0,
            ResourceUrl: root.TryGetProperty("resource_url", out JsonElement r) ? r.GetString() : null,
            Uri: root.TryGetProperty("uri", out JsonElement u) ? u.GetString() : null,
            Tracklist: ExtractTracklist(root)
        );
    }

    public async Task<List<DiscogsVersion>> GetVersionsAsync(int masterId, int maxResults = 100)
    {
        string url = $"/masters/{masterId}/versions?per_page={Math.Min(maxResults, 100)}";
        using HttpResponseMessage response = await ExecuteWithRetryAsync(() => Client.GetAsync(url));
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        
        if (!doc.RootElement.TryGetProperty("versions", out JsonElement versions)) return [];

        List<DiscogsVersion> items = [];
        foreach (JsonElement item in versions.EnumerateArray())
        {
            items.Add(new DiscogsVersion(
                Id: item.GetProperty("id").GetInt32(),
                Title: item.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? "" : "",
                Format: item.TryGetProperty("format", out JsonElement f) ? f.GetString() : null,
                Label: item.TryGetProperty("label", out JsonElement l) ? l.GetString() : null,
                Country: item.TryGetProperty("country", out JsonElement c) ? c.GetString() : null,
                Year: item.TryGetProperty("released", out JsonElement y) ? ParseNullableInt(y) : null,
                CatalogNumber: item.TryGetProperty("catno", out JsonElement cat) ? cat.GetString() : null,
                ResourceUrl: item.TryGetProperty("resource_url", out JsonElement r) ? r.GetString() : null,
                Thumb: item.TryGetProperty("thumb", out JsonElement th) ? th.GetString() : null
            ));
            if (items.Count >= maxResults) break;
        }
        return items;
    }

    public async Task<DiscogsRelease?> GetArtistReleasesFirstAsync(string artistName)
    {
        List<DiscogsSearchResult> results = await SearchAsync(artist: artistName, maxResults: 1);
        if (results.Count == 0) return null;
        return await GetReleaseAsync(results[0].Id);
    }

    static async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> action)
    {
        int retries = 0;
        int maxRetries = 5;
        TimeSpan delay = TimeSpan.FromSeconds(3);

        while (true)
        {
            await GlobalRateLimiter.Lock.WaitAsync();
            HttpResponseMessage response;
            try
            {
                await Task.Delay(1500); // Rate limit protection
                response = await action();
            }
            finally
            {
                GlobalRateLimiter.Lock.Release();
            }

            if ((int)response.StatusCode != 429) return response;

            retries++;
            if (retries > maxRetries) return response;

            await Task.Delay(delay);
            delay *= 2;
        }
    }

    static string? ExtractArtist(JsonElement item) =>
        item.TryGetProperty("title", out JsonElement title) && title.GetString() is { } t && t.Contains(" - ")
            ? t.Split(" - ")[0].Trim()
            : null;

    static List<string> ExtractArtistsList(JsonElement root)
    {
        if (!root.TryGetProperty("artists", out JsonElement artists)) return [];
        return artists.EnumerateArray()
            .Select(a => a.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    static List<string> ExtractStringArray(JsonElement root, string arrayName, string propertyName)
    {
        if (!root.TryGetProperty(arrayName, out JsonElement arr)) return [];
        return arr.EnumerateArray()
            .Select(a => a.TryGetProperty(propertyName, out JsonElement p) ? p.GetString() ?? "" : "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    static List<string> ExtractStringArraySimple(JsonElement root, string arrayName)
    {
        if (!root.TryGetProperty(arrayName, out JsonElement arr)) return [];
        return arr.EnumerateArray()
            .Select(a => a.GetString() ?? "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    static List<string> ExtractFormats(JsonElement root)
    {
        if (!root.TryGetProperty("formats", out JsonElement formats)) return [];
        List<string> result = [];
        foreach (JsonElement f in formats.EnumerateArray())
        {
            string name = f.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "";
            if (!string.IsNullOrEmpty(name)) result.Add(name);
        }
        return result;
    }

    static List<DiscogsTrack> ExtractTracklist(JsonElement root)
    {
        if (!root.TryGetProperty("tracklist", out JsonElement tracklist)) return [];
        List<DiscogsTrack> tracks = [];
        foreach (JsonElement t in tracklist.EnumerateArray())
        {
            tracks.Add(new DiscogsTrack(
                Position: t.TryGetProperty("position", out JsonElement p) ? p.GetString() ?? "" : "",
                Title: t.TryGetProperty("title", out JsonElement ti) ? ti.GetString() ?? "" : "",
                Duration: t.TryGetProperty("duration", out JsonElement d) ? d.GetString() ?? "" : "",
                ExtraArtists: null
            ));
        }
        return tracks;
    }

    static int? ParseNullableInt(JsonElement elem)
    {
        if (elem.ValueKind == JsonValueKind.Number) return elem.GetInt32();
        if (elem.ValueKind == JsonValueKind.String && int.TryParse(elem.GetString(), out int val)) return val;
        return null;
    }
}

public class MusicBrainzService
{
    readonly Query Query;
    readonly string AppName;
    readonly string AppVersion;
    readonly string Contact;

    public MusicBrainzService(string appName = "PlaygroundApp", string appVersion = "1.0", string contact = "user@example.com")
    {
        AppName = appName;
        AppVersion = appVersion;
        Contact = contact;
        Query = new Query(appName, appVersion, contact);
    }

    async Task<T> ExecuteWithLockAsync<T>(Func<Task<T>> action)
    {
        int retries = 0;
        int maxRetries = 5;
        TimeSpan delay = TimeSpan.FromSeconds(3);

        while (true)
        {
            await GlobalRateLimiter.Lock.WaitAsync();
            try
            {
                await Task.Delay(1500); // Rate limit protection
                return await action();
            }
            catch (Exception ex) when (retries < maxRetries && (ex.Message.Contains("429") || ex.Message.Contains("503") || ex.InnerException is System.IO.IOException || ex is System.Net.Http.HttpRequestException))
            {
                retries++;
                await Task.Delay(delay);
                delay *= 2;
            }
            finally
            {
                GlobalRateLimiter.Lock.Release();
            }
        }
    }

    public async Task<List<MusicBrainzSearchResult>> SearchReleasesAsync(string? artist = null, string? release = null, int? year = null, int maxResults = 25)
    {
        string query = BuildQuery(artist, release, year);
        if (string.IsNullOrEmpty(query)) return [];

        return await ExecuteWithLockAsync(async () => 
        {
            ISearchResults<ISearchResult<IRelease>> results = await Query.FindReleasesAsync(query, maxResults);
            return results.Results
                .Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Title,
                    Artist: r.Item.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                    Year: r.Item.Date?.Year,
                    Country: r.Item.Country,
                    Status: r.Item.Status,
                    Disambiguation: r.Item.Disambiguation
                ))
                .ToList();
        });
    }

    public async Task<MusicBrainzRelease?> GetReleaseAsync(Guid releaseId, bool includeTracks = true)
    {
        return await ExecuteWithLockAsync(async () =>
        {
            Include inc = Include.ArtistCredits;
            if (includeTracks) inc |= Include.Recordings | Include.Media;

            IRelease? release = await Query.LookupReleaseAsync(releaseId, inc);
            if (release is null) return null;

            List<MusicBrainzTrack> tracks = [];
            if (release.Media is { } media)
            {
                foreach (IMedium medium in media)
                {
                    if (medium.Tracks is not { } mediumTracks) continue;
                    foreach (ITrack track in mediumTracks)
                    {
                        tracks.Add(new MusicBrainzTrack(
                            Id: track.Id,
                            Title: track.Title ?? track.Recording?.Title ?? "",
                            Position: track.Position ?? 0,
                            Length: track.Length,
                            RecordingId: track.Recording?.Id
                        ));
                    }
                }
            }

            return new MusicBrainzRelease(
                Id: release.Id,
                Title: release.Title ?? "",
                Artist: release.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                ArtistCredit: release.ArtistCredit is { } credits ? string.Join(", ", credits.Select(a => a.Artist?.Name ?? "")) : null,
                Date: release.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
                Country: release.Country,
                Status: release.Status,
                Barcode: release.Barcode,
                Disambiguation: release.Disambiguation,
                Tracks: tracks
            );
        });
    }

    public async Task<List<MusicBrainzSearchResult>> SearchReleaseGroupsAsync(string? artist = null, string? releaseGroup = null, int maxResults = 25)
    {
        string query = "";
        if (!string.IsNullOrWhiteSpace(artist)) query += $"artist:\"{artist}\"";
        if (!string.IsNullOrWhiteSpace(releaseGroup))
        {
            if (query.Length > 0) query += " AND ";
            query += $"releasegroup:\"{releaseGroup}\"";
        }
        if (string.IsNullOrEmpty(query)) return [];

        return await ExecuteWithLockAsync(async () =>
        {
            ISearchResults<ISearchResult<IReleaseGroup>> results = await Query.FindReleaseGroupsAsync(query, maxResults);
            return results.Results
                .Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Title,
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
        return await ExecuteWithLockAsync(async () =>
        {
            IReleaseGroup? rg = await Query.LookupReleaseGroupAsync(releaseGroupId, Include.ArtistCredits | Include.Releases);
            if (rg is null) return null;

            return new MusicBrainzReleaseGroup(
                Id: rg.Id,
                Title: rg.Title ?? "",
                Artist: rg.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
                PrimaryType: rg.PrimaryType,
                SecondaryTypes: rg.SecondaryTypes?.ToList(),
                FirstReleaseDate: rg.FirstReleaseDate?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
                ReleaseCount: rg.Releases?.Count ?? 0,
                Disambiguation: rg.Disambiguation
            );
        });
    }

    public async Task<List<MusicBrainzSearchResult>> SearchArtistsAsync(string artist, int maxResults = 25)
    {
        return await ExecuteWithLockAsync(async () =>
        {
            ISearchResults<ISearchResult<IArtist>> results = await Query.FindArtistsAsync($"artist:\"{artist}\"", maxResults);
            return results.Results
                .Select(r => new MusicBrainzSearchResult(
                    Id: r.Item.Id,
                    Title: r.Item.Name,
                    Artist: r.Item.Name,
                    Year: r.Item.LifeSpan?.Begin?.Year,
                    Country: r.Item.Country,
                    Status: r.Item.Type,
                    Disambiguation: r.Item.Disambiguation
                ))
                .ToList();
        });
    }

    static string BuildQuery(string? artist, string? release, int? year)
    {
        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(artist)) parts.Add($"artist:\"{artist}\"");
        if (!string.IsNullOrWhiteSpace(release)) parts.Add($"release:\"{release}\"");
        if (year.HasValue) parts.Add($"date:{year}");
        return string.Join(" AND ", parts);
    }
}

public class MusicMetadataService(string? DiscogsToken = null)
{
    public DiscogsService Discogs { get; } = new(new DiscogsClientConfig(DiscogsToken));
    public MusicBrainzService MusicBrainz { get; } = new();

    public async Task<MusicSearchResult?> SearchAsync(string title, string? artist = null)
    {
        List<MusicBrainzSearchResult> mbResults = await MusicBrainz.SearchReleasesAsync(artist: artist, release: title, maxResults: 5);
        
        if (mbResults.Count > 0)
        {
            MusicBrainzSearchResult first = mbResults[0];
            return new MusicSearchResult(
                Title: first.Title ?? title,
                Artist: first.Artist ?? artist ?? "Unknown",
                Year: first.Year,
                Source: "MusicBrainz",
                ExternalId: first.Id.ToString()
            );
        }

        if (!string.IsNullOrEmpty(DiscogsToken))
        {
            List<DiscogsSearchResult> discogsResults = await Discogs.SearchAsync(artist: artist, track: title, maxResults: 5);
            if (discogsResults.Count > 0)
            {
                DiscogsSearchResult first = discogsResults[0];
                return new MusicSearchResult(
                    Title: first.Title ?? title,
                    Artist: first.Artist ?? artist ?? "Unknown",
                    Year: first.Year,
                    Source: "Discogs",
                    ExternalId: first.Id.ToString()
                );
            }
        }

        return null;
    }

    public async Task<(List<DiscogsSearchResult> Discogs, List<MusicBrainzSearchResult> MusicBrainz)> SearchBothAsync(string? artist = null, string? release = null, int? year = null)
    {
        // Sequential execution to respect rate limits and single-threaded requirement
        List<DiscogsSearchResult> discogsResults = await Discogs.SearchAsync(artist: artist, release: release, year: year);
        List<MusicBrainzSearchResult> mbResults = await MusicBrainz.SearchReleasesAsync(artist: artist, release: release, year: year);
        
        return (discogsResults, mbResults);
    }
}
