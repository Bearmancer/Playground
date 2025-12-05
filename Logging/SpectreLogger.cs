namespace Playground.Logging;

public static class SpectreLogger
{
    static string Escape(string text) => text.Replace("[", "[[").Replace("]", "]]");

    public static void Info(string message) =>
        AnsiConsole.MarkupLine($"[cyan]Info:[/] {Escape(message)}");

    public static void Success(string message) =>
        AnsiConsole.MarkupLine($"[green]Success:[/] {Escape(message)}");

    public static void Warning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]Warning:[/] {Escape(message)}");

    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[red]Error:[/] {Escape(message)}");

    public static void Debug(string message) =>
        AnsiConsole.MarkupLine($"[dim]Debug:[/] {Escape(message)}");

    public static void Starting(string operation) =>
        AnsiConsole.MarkupLine($"[blue]→[/] {Escape(operation)}");

    public static void Complete(string operation) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {Escape(operation)}");

    public static void Failed(string operation) =>
        AnsiConsole.MarkupLine($"[red]✗[/] {Escape(operation)}");

    public static void KeyValue(string key, string value) =>
        AnsiConsole.MarkupLine($"[cyan]{Escape(key)}:[/] {Escape(value)}");

    public static void Table(string title, Dictionary<string, string> data)
    {
        Table table = new();
        table.Title = new TableTitle(Escape(title));
        table.AddColumn(new TableColumn("[cyan]Key[/]"));
        table.AddColumn(new TableColumn("[cyan]Value[/]"));

        foreach (KeyValuePair<string, string> kvp in data)
            table.AddRow(Escape(kvp.Key), Escape(kvp.Value));

        AnsiConsole.Write(table);
    }

    public static void Table(string title, IEnumerable<string[]> rows, params string[] columns)
    {
        Table table = new();
        table.Title = new TableTitle(Escape(title));

        foreach (string col in columns)
            table.AddColumn(new TableColumn($"[cyan]{Escape(col)}[/]"));

        foreach (string[] row in rows)
            table.AddRow(row.Select(Escape).ToArray());

        AnsiConsole.Write(table);
    }

    public static void Rule(string text) =>
        AnsiConsole.Write(new Rule($"[bold cyan]{Escape(text)}[/]"));

    public static void Figlet(string text, Color? color = null)
    {
        FigletText figlet = new(text);
        if (color.HasValue)
            figlet.Color = color.Value;
        AnsiConsole.Write(figlet);
    }

    public static Progress Progress() => AnsiConsole.Progress();

    public static void Write(IRenderable renderable) => AnsiConsole.Write(renderable);

    public static void MarkupLine(string markup) => AnsiConsole.MarkupLine(markup);

    public static void WriteLine(string text) => AnsiConsole.WriteLine(text);

    public static void Dim(string text) => AnsiConsole.MarkupLine($"[dim]{Escape(text)}[/]");

    public static void Tip(string text) => AnsiConsole.MarkupLine($"[dim]Tip:[/] {Escape(text)}");

    public static void Link(int number, string url, int maxLength = 80)
    {
        string truncated = url.Length <= maxLength ? url : url[..(maxLength - 3)] + "...";
        AnsiConsole.MarkupLine($"  [blue][link={url}]{number}. {Escape(truncated)}[/][/]");
    }

    public static void Clear() => AnsiConsole.Clear();

    public static void NewLine() => AnsiConsole.WriteLine();

    public static Panel CreatePanel(string content, string header) =>
        new Panel(Escape(content)).Header(Escape(header)).Expand();

    // ═══════════════════════════════════════════════════════════════════════════
    // Help Formatting (.NET CLI style)
    // ═══════════════════════════════════════════════════════════════════════════

    public static void Section(string title) =>
        AnsiConsole.MarkupLine($"[bold white]{Escape(title)}[/]");

    public static void HelpUsage(string usage) => AnsiConsole.MarkupLine($"  {Escape(usage)}");

    public static void HelpExample(string example) =>
        AnsiConsole.MarkupLine($"  {Escape(example)}");

    public static Table CreateHelpTable()
    {
        Table table = new Table().NoBorder().HideHeaders();
        table.AddColumn(new TableColumn("").PadRight(4));
        table.AddColumn("");
        return table;
    }

    public static void AddHelpRow(Table table, string command, string description) =>
        table.AddRow($"[green]{Escape(command)}[/]", Escape(description));

    /// <summary>
    /// Renders an option in .NET CLI style:
    /// -s, --source &lt;SOURCE&gt;    Description. Allowed values are x, y, z. [default: x]
    /// </summary>
    public static void HelpOption(
        string shortFlag,
        string longFlag,
        string? valuePlaceholder,
        string description,
        string[]? allowedValues = null,
        string? defaultValue = null
    )
    {
        // Build the option signature
        string sig = string.IsNullOrEmpty(shortFlag)
            ? $"    --{longFlag}"
            : $"  -{shortFlag}, --{longFlag}";

        if (!string.IsNullOrEmpty(valuePlaceholder))
            sig += $" [cyan]<{valuePlaceholder.ToUpperInvariant()}>[/]";

        // Build the description with inline metadata
        string desc = Escape(description);

        if (allowedValues is { Length: > 0 })
            desc += $" [dim]Allowed values are {string.Join(", ", allowedValues)}.[/]";

        if (!string.IsNullOrEmpty(defaultValue))
            desc += $" [dim][[default: {Escape(defaultValue)}]][/]";

        // Calculate padding for alignment (target: 44 chars for option column)
        int rawSigLen = System
            .Text.RegularExpressions.Regex.Replace(sig, @"\[/?[^\]]+\]", "")
            .Length;
        int padding = Math.Max(2, 44 - rawSigLen);

        AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(' ', padding)}{desc}");
    }

    /// <summary>
    /// Simplified overload for flags without values.
    /// </summary>
    public static void HelpFlag(
        string shortFlag,
        string longFlag,
        string description,
        bool defaultValue = false
    )
    {
        string sig = string.IsNullOrEmpty(shortFlag)
            ? $"    --{longFlag}"
            : $"  -{shortFlag}, --{longFlag}";

        string desc = Escape(description);
        desc += $" [dim][[default: {(defaultValue ? "True" : "False")}]][/]";

        int rawSigLen = System
            .Text.RegularExpressions.Regex.Replace(sig, @"\[/?[^\]]+\]", "")
            .Length;
        int padding = Math.Max(2, 44 - rawSigLen);

        AnsiConsole.MarkupLine($"[yellow]{sig}[/]{new string(' ', padding)}{desc}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Result Tables (with row separators and consistent styling)
    // ═══════════════════════════════════════════════════════════════════════════

    public static Table CreateResultTable(string title, string titleColor = "blue")
    {
        Table table = new()
        {
            Title = new TableTitle($"[bold {titleColor}]{Escape(title)}[/]"),
            Border = TableBorder.Rounded,
            ShowRowSeparators = true,
        };
        return table;
    }

    public static void AddResultColumn(Table table, string header) =>
        table.AddColumn($"[bold]{Escape(header)}[/]");

    /// <summary>
    /// Adds a row with consistent steel blue color.
    /// </summary>
    public static void AddResultRow(Table table, params string[] cells)
    {
        string[] formatted = cells.Select(c => $"[steelblue1]{Markup.Escape(c)}[/]").ToArray();
        table.AddRow(formatted);
    }
}
