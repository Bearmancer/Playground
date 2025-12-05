namespace Playground.Utilities;

using Playground.Services;
using Spectre.Console;

public static class SpectreTables
{
    public static Table BuildReleaseTable(UnifiedRelease release)
    {
        Table table = new();
        table.AddColumn("Artist Name");
        table.AddColumn("Album Name");
        table.AddColumn("Track Title");
        table.AddColumn("Duration");
        table.AddColumn("Release Year");

        string album = release.Title;
        string artist = release.Artist ?? "Unknown";
        string year = release.Year?.ToString() ?? "Unknown";

        if (release.Tracks.Count == 0)
        {
            table.AddRow(artist, album, "", "", year);
            return table;
        }

        foreach (UnifiedTrack track in release.Tracks)
        {
            string duration = track.Duration.HasValue
                ? track.Duration.Value.ToString("m\\:ss")
                : "";
            table.AddRow(artist, album, track.Title, duration, year);
        }

        return table;
    }
}
