namespace Playground.Commands;

using System.Text.RegularExpressions;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Playground.Logging;
using Playground.Services;
using Spectre.Console;

[Command(
    "mail",
    Description = "Create a temporary Mail.tm inbox and monitor for incoming messages."
)]
public sealed class MailCommand : ICommand
{
    [CommandOption(
        "refresh-seconds",
        'r',
        Description = "Interval between inbox refresh cycles. [default: 10]"
    )]
    public int? RefreshSeconds { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        SpectreLogger.Rule("Mail.tm");
        int refreshSeconds = RefreshSeconds ?? 10;
        MailTmService service = new();

        MailTmAccount account = await service.CreateAccountAsync();
        SpectreLogger.Success($"Created account: {account.Address}");
        SpectreLogger.Info("Watching inbox. Press Ctrl+C to exit.");

        while (true)
        {
            SpectreLogger.Clear();
            SpectreLogger.Rule("Mail.tm");
            SpectreLogger.Success($"Watching: {account.Address}");

            List<MailTmMessage> inbox = await service.GetInboxAsync();
            SpectreLogger.KeyValue("Messages", inbox.Count.ToString());

            if (inbox.Count == 0)
            {
                SpectreLogger.Info("Inbox empty. Waiting for new mail...");
                await Task.Delay(TimeSpan.FromSeconds(refreshSeconds));
                continue;
            }

            Table table = SpectreLogger.CreateResultTable("Inbox", "cyan");
            SpectreLogger.AddResultColumn(table, "#");
            SpectreLogger.AddResultColumn(table, "From");
            SpectreLogger.AddResultColumn(table, "Subject");
            SpectreLogger.AddResultColumn(table, "Received");

            int index = 0;
            foreach (MailTmMessage message in inbox)
            {
                SpectreLogger.AddResultRow(
                    table,
                    (index + 1).ToString(),
                    message.From?.Address ?? "(unknown)",
                    message.Subject,
                    message.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss")
                );
                index++;
            }

            SpectreLogger.Write(table);

            List<string> choices =
            [
                .. inbox.Select((msg, i) => $"{i + 1}. {Truncate(msg.Subject, 40)}"),
            ];
            choices.Add("[Refresh inbox]");
            choices.Add("[Quit]");

            string selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a message to read:")
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (selection == "[Quit]")
                return;

            if (selection == "[Refresh inbox]")
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                continue;
            }

            int messageIndex = choices.IndexOf(selection);
            if (messageIndex >= 0 && messageIndex < inbox.Count)
            {
                MailTmMessage message = inbox[messageIndex];
                MailTmMessage full = await service.ReadMessageAsync(message.Id);

                SpectreLogger.Clear();
                SpectreLogger.Rule("Message Detail");
                SpectreLogger.KeyValue("From", full.From?.Address ?? "(unknown)");
                SpectreLogger.KeyValue("Subject", full.Subject);
                SpectreLogger.KeyValue("Received", full.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss"));
                SpectreLogger.NewLine();

                string body = full.Text ?? "(no text content)";
                SpectreLogger.Write(SpectreLogger.CreatePanel(body, "Body"));

                List<string> links = ExtractUrls(body);
                if (links.Count > 0)
                {
                    SpectreLogger.NewLine();
                    SpectreLogger.Info($"Found {links.Count} link(s) in message:");

                    for (int i = 0; i < links.Count; i++)
                        SpectreLogger.Link(i + 1, links[i]);

                    SpectreLogger.NewLine();
                    SpectreLogger.Tip(
                        "Copy a link and paste in browser, or use Start-Process \"<url>\" in PowerShell."
                    );
                }

                SpectreLogger.NewLine();
                SpectreLogger.Dim("Press Enter to return to inbox...");
                await console.Input.ReadLineAsync();
            }
        }
    }

    static List<string> ExtractUrls(string text)
    {
        List<string> urls = [];
        MatchCollection matches = Regex.Matches(text, @"https?://[^\s<>""'\)\]]+");
        foreach (Match match in matches)
            urls.Add(match.Value);
        return urls;
    }

    static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}
