namespace Playground.Commands;

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Playground.Logging;
using Spectre.Console;

[Command(
    Description = "Playground CLI - A developer toolkit for music metadata and temporary email."
)]
public sealed class RootCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        SpectreLogger.Rule("Playground CLI");
        SpectreLogger.NewLine();

        SpectreLogger.Section("USAGE");
        SpectreLogger.HelpUsage("playground <command> [options]");
        SpectreLogger.NewLine();

        SpectreLogger.Section("COMMANDS");
        Table table = SpectreLogger.CreateHelpTable();
        SpectreLogger.AddHelpRow(
            table,
            "mail",
            "Create a temporary inbox and monitor for incoming messages."
        );
        SpectreLogger.AddHelpRow(
            table,
            "music",
            "Search and retrieve music metadata from online databases."
        );
        SpectreLogger.AddHelpRow(
            table,
            "cli compare",
            "Execute a comparative benchmark of CLI framework runners."
        );
        SpectreLogger.Write(table);
        SpectreLogger.NewLine();

        SpectreLogger.Section("OPTIONS");
        SpectreLogger.HelpFlag("?", "help", "Show command line help.");
        SpectreLogger.NewLine();

        SpectreLogger.Section("EXAMPLES");
        SpectreLogger.HelpExample("playground mail");
        SpectreLogger.HelpExample("playground music search \"Pink Floyd\"");
        SpectreLogger.HelpExample("playground music search --source discogs --album \"Dark Side\"");

        return ValueTask.CompletedTask;
    }
}
