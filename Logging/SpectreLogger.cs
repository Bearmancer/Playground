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
        {
            table.AddRow(Escape(kvp.Key), Escape(kvp.Value));
        }

        AnsiConsole.Write(table);
    }

    public static void Table(string title, IEnumerable<string[]> rows, params string[] columns)
    {
        Table table = new();
        table.Title = new TableTitle(Escape(title));

        foreach (string col in columns)
        {
            table.AddColumn(new TableColumn($"[cyan]{Escape(col)}[/]"));
        }

        foreach (string[] row in rows)
        {
            table.AddRow(row.Select(Escape).ToArray());
        }

        AnsiConsole.Write(table);
    }

    public static void Rule(string text) =>
        AnsiConsole.Write(new Rule($"[bold cyan]{Escape(text)}[/]"));

    public static void Figlet(string text, Color? color = null)
    {
        FigletText figlet = new(text);
        if (color.HasValue)
        {
            figlet.Color = color.Value;
        }
        AnsiConsole.Write(figlet);
    }

    public static Progress Progress() => AnsiConsole.Progress();

    public static void Write(IRenderable renderable) => AnsiConsole.Write(renderable);

    public static void MarkupLine(string markup) => AnsiConsole.MarkupLine(markup);

    public static void WriteLine(string text) => AnsiConsole.WriteLine(text);
}
