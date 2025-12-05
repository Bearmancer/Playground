namespace Playground.Commands;

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Playground.Logging;
using Spectre.Console;

[Command("music", Description = "Search and retrieve music metadata from online databases.")]
public sealed class MusicCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        SpectreLogger.Rule("Music Metadata");
        SpectreLogger.NewLine();

        SpectreLogger.Section("USAGE");
        SpectreLogger.HelpUsage("playground music <subcommand> [options]");
        SpectreLogger.NewLine();

        SpectreLogger.Section("SUBCOMMANDS");
        Table table = SpectreLogger.CreateHelpTable();
        SpectreLogger.AddHelpRow(
            table,
            "search",
            "Query Discogs and MusicBrainz for music releases."
        );
        SpectreLogger.Write(table);
        SpectreLogger.NewLine();

        SpectreLogger.Section("EXAMPLES");
        SpectreLogger.HelpExample("playground music search \"Dark Side of the Moon\"");
        SpectreLogger.HelpExample("playground music search \"Pink Floyd\" --source discogs");
        SpectreLogger.HelpExample(
            "playground music search --artist \"Beatles\" --album \"Abbey Road\""
        );

        return ValueTask.CompletedTask;
    }
}
