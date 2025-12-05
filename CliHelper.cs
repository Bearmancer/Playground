namespace Playground;

public class CommandLineParser
{
    public string? Command { get; set; }
    public Dictionary<string, string> Options { get; } = [];
    public List<string> Arguments { get; } = [];

    public static CommandLineParser Parse(string[] args)
    {
        var parser = new CommandLineParser();

        if (args.Length == 0)
        {
            return parser;
        }

        parser.Command = args[0];

        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.StartsWith("--"))
            {
                string keyValue = arg.Substring(2);
                string[] parts = keyValue.Split('=', 2);

                parser.Options[parts[0]] = parts.Length > 1 ? parts[1] : "true";
            }
            else if (arg.StartsWith("-"))
            {
                string key = arg.Substring(1);
                parser.Options[key] = "true";
            }
            else
            {
                parser.Arguments.Add(arg);
            }
        }

        return parser;
    }

    public string? GetOption(string key, string? defaultValue = null)
    {
        return Options.TryGetValue(key, out string? value) ? value : defaultValue;
    }

    public bool HasOption(string key)
    {
        return Options.ContainsKey(key);
    }
}

public static class CliHelper
{
    public static void ShowHelp()
    {
        var rule = new Rule("[bold cyan]Playground CLI[/]");
        AnsiConsole.Write(rule);

        AnsiConsole.MarkupLine("\n[bold]Usage:[/]");
        AnsiConsole.MarkupLine("  playground [[COMMAND]] [[OPTIONS]]\n");

        AnsiConsole.MarkupLine("[bold]Commands:[/]");

        var table = new Table();
        table.BorderStyle(Style.Parse("cyan"));
        table.AddColumn("[bold]Command[/]");
        table.AddColumn("[bold]Description[/]");

        table.AddRow("help", "Show this help message");
        table.AddRow("scrape", "Scrape Bowie discography");
        table.AddRow("mail", "Test mail.tm service");
        table.AddRow("metadata", "Search music metadata");

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("\n[bold]Global Options:[/]");
        AnsiConsole.MarkupLine("  [cyan]--verbose[/]          Enable verbose logging");
        AnsiConsole.MarkupLine("  [cyan]--quiet[/]            Suppress non-essential output");
        AnsiConsole.MarkupLine("  [cyan]--format=<fmt>[/]     Output format (text, json)");

        AnsiConsole.MarkupLine("\n[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  [dim]playground help[/]");
        AnsiConsole.MarkupLine("  [dim]playground scrape --verbose[/]");
        AnsiConsole.MarkupLine("  [dim]playground mail --quiet[/]");

        AnsiConsole.Write(new Rule());
    }

    public static void ShowCommandHelp(string command)
    {
        switch (command.ToLower())
        {
            case "scrape":
                AnsiConsole.MarkupLine("[bold cyan]Scrape Command[/]");
                AnsiConsole.MarkupLine("Scrapes David Bowie discography from the web");
                AnsiConsole.MarkupLine("\n[bold]Options:[/]");
                AnsiConsole.MarkupLine("  [cyan]--output=<path>[/]   Output file path");
                AnsiConsole.MarkupLine("  [cyan]--format=<fmt>[/]    csv, json");
                break;

            case "mail":
                AnsiConsole.MarkupLine("[bold cyan]Mail Command[/]");
                AnsiConsole.MarkupLine("Test mail.tm temporary email service");
                AnsiConsole.MarkupLine("\n[bold]Options:[/]");
                AnsiConsole.MarkupLine("  [cyan]--action=<act>[/]    create, read, delete");
                AnsiConsole.MarkupLine("  [cyan]--email=<addr>[/]    Email address");
                break;

            case "metadata":
                AnsiConsole.MarkupLine("[bold cyan]Metadata Command[/]");
                AnsiConsole.MarkupLine("Search music metadata via Discogs/MusicBrainz");
                AnsiConsole.MarkupLine("\n[bold]Options:[/]");
                AnsiConsole.MarkupLine("  [cyan]--title=<name>[/]    Song title");
                AnsiConsole.MarkupLine("  [cyan]--artist=<name>[/]   Artist name");
                break;

            default:
                AnsiConsole.Write(new Markup("[yellow]Unknown command:[/] "));
                Console.WriteLine(command);
                break;
        }
    }
}
