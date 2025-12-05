namespace Playground.Commands;

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Playground.Logging;
using Playground.Services;
using Spectre.Console;

[Command(
    "music search",
    Description = "Query Discogs and MusicBrainz databases for music releases."
)]
public sealed class MusicSearchCommand : ICommand
{
    [CommandParameter(
        0,
        Name = "query",
        Description = "Search term (artist, album, or any text). Optional if using field-specific options.",
        IsRequired = false
    )]
    public string? Query { get; init; }

    [CommandOption("track", 't', Description = "Filter by track title.")]
    public string? Track { get; init; }

    [CommandOption("artist", 'a', Description = "Filter by artist or band name.")]
    public string? Artist { get; init; }

    [CommandOption("album", 'l', Description = "Filter by album or release title.")]
    public string? Album { get; init; }

    [CommandOption(
        "source",
        's',
        Description = "Metadata source to query. Allowed values are both, musicbrainz, discogs."
    )]
    public string Source { get; init; } = "both";

    [CommandOption(
        "sort",
        Description = "Field to sort results by. Allowed values are type, artist, album, year, label."
    )]
    public string Sort { get; init; } = "none";

    [CommandOption(
        "more",
        'm',
        Description = "Prompt to load additional results after initial display."
    )]
    public bool LoadMoreEnabled { get; init; } = false;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        SpectreLogger.Rule("Music Search");

        try
        {
            string? searchQuery = Query;
            string? resolvedArtist = Artist;
            string? resolvedAlbum = Album;
            string? resolvedTrack = Track;

            bool hasInput =
                !string.IsNullOrWhiteSpace(searchQuery)
                || !string.IsNullOrWhiteSpace(Artist)
                || !string.IsNullOrWhiteSpace(Album)
                || !string.IsNullOrWhiteSpace(Track);

            if (!hasInput)
                searchQuery = AnsiConsole.Prompt(new TextPrompt<string>("Enter search term:"));

            string resolvedToken = Environment.GetEnvironmentVariable("DISCOGS_USER_TOKEN") ?? "";

            string resolvedSource = Source.ToLowerInvariant();

            if (
                (resolvedSource == "discogs" || resolvedSource == "both")
                && string.IsNullOrWhiteSpace(resolvedToken)
            )
            {
                SpectreLogger.Warning(
                    "DISCOGS_USER_TOKEN not set; Discogs results will be unavailable."
                );
                if (resolvedSource == "discogs")
                {
                    SpectreLogger.Error("Cannot proceed with Discogs-only search without token.");
                    return;
                }
                resolvedSource = "musicbrainz";
            }

            MusicMetadataService service = new(resolvedToken);

            const int DEFAULT_MAX_RESULTS = 50;
            const int MAX_RESULTS_STEP = 25;
            const int MAX_RESULTS_CAP = 100;

            int currentMax = DEFAULT_MAX_RESULTS;
            string? sortBy = Sort == "none" ? null : Sort;

            while (true)
            {
                SpectreLogger.Info($"Fetching up to {currentMax} results per source...");

                MusicSearchQuery query = new(
                    Artist: resolvedArtist ?? searchQuery,
                    Album: resolvedAlbum,
                    Track: resolvedTrack,
                    MaxResults: currentMax,
                    SortBy: sortBy
                );

                (List<DiscogsSearchResult>? discogs, List<MusicBrainzSearchResult> musicBrainz) =
                    await service.SearchBothAsync(query, resolvedSource);

                bool any = false;

                if (musicBrainz.Count > 0)
                {
                    any = true;
                    Table mbTable = SpectreLogger.CreateResultTable("MusicBrainz", "blue");
                    SpectreLogger.AddResultColumn(mbTable, "Type");
                    SpectreLogger.AddResultColumn(mbTable, "Title");
                    SpectreLogger.AddResultColumn(mbTable, "Artist");
                    SpectreLogger.AddResultColumn(mbTable, "Year");
                    SpectreLogger.AddResultColumn(mbTable, "Country");

                    foreach (MusicBrainzSearchResult item in musicBrainz.Take(currentMax))
                    {
                        SpectreLogger.AddResultRow(
                            mbTable,
                            ClassifyReleaseType(item.Status),
                            item.Title,
                            item.Artist ?? "Unknown",
                            item.Year?.ToString() ?? "",
                            item.Country ?? ""
                        );
                    }

                    SpectreLogger.Write(mbTable);
                    SpectreLogger.NewLine();
                }

                if (discogs is not null && discogs.Count > 0)
                {
                    any = true;
                    Table discogsTable = SpectreLogger.CreateResultTable("Discogs", "orange1");
                    SpectreLogger.AddResultColumn(discogsTable, "Type");
                    SpectreLogger.AddResultColumn(discogsTable, "Title");
                    SpectreLogger.AddResultColumn(discogsTable, "Artist");
                    SpectreLogger.AddResultColumn(discogsTable, "Year");
                    SpectreLogger.AddResultColumn(discogsTable, "Label");

                    foreach (DiscogsSearchResult item in discogs.Take(currentMax))
                    {
                        SpectreLogger.AddResultRow(
                            discogsTable,
                            ClassifyReleaseType(item.Format),
                            item.Title ?? "",
                            item.Artist ?? "",
                            item.Year?.ToString() ?? "",
                            TruncateLabel(item.Label, 30)
                        );
                    }

                    SpectreLogger.Write(discogsTable);
                }

                if (!any)
                {
                    SpectreLogger.Warning("No results found.");
                    return;
                }

                if (!LoadMoreEnabled || currentMax >= MAX_RESULTS_CAP)
                {
                    if (currentMax >= MAX_RESULTS_CAP)
                        SpectreLogger.Info("Maximum result limit reached.");
                    return;
                }

                bool loadMore = AnsiConsole.Confirm("Load more results?", defaultValue: false);

                if (!loadMore)
                    return;

                currentMax = Math.Min(currentMax + MAX_RESULTS_STEP, MAX_RESULTS_CAP);
            }
        }
        catch (Exception ex)
        {
            SpectreLogger.Error($"{ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException is not null)
                SpectreLogger.Error($"  Caused by: {ex.InnerException.Message}");
        }
    }

    static string ClassifyReleaseType(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return "—";

        string lower = format.ToLowerInvariant();

        if (lower.Contains("single"))
            return "Single";
        if (lower.Contains("ep") || lower.Contains("e.p."))
            return "EP";
        if (lower.Contains("compilation") || lower.Contains("comp"))
            return "Comp";
        if (lower.Contains("anthology"))
            return "Anthology";
        if (lower.Contains("lp") || lower.Contains("album"))
            return "LP";
        if (lower == "official")
            return "Album";
        if (lower.Contains("bootleg") || lower.Contains("live"))
            return "Live";
        if (lower.Contains("promo"))
            return "Promo";

        return "—";
    }

    static string TruncateLabel(string? label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "";

        string[] labels = label.Split(", ", StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length <= 2)
            return label.Length <= maxLength ? label : label[..(maxLength - 3)] + "...";

        string first = labels[0];
        return $"{(first.Length <= 20 ? first : first[..17] + "...")} (+{labels.Length - 1} more)";
    }
}
