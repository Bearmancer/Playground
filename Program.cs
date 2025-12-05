namespace Playground;

using CliFx;
using Playground.Commands;
using Playground.Logging;

internal sealed class Program
{
    static async Task<int> Main(string[] args)
    {
        SpectreLogger.Info("Starting Playground CLI (CliFx)");

        string[] normalizedArgs = [.. args.Select(arg => arg == "-?" ? "--help" : arg)];

        CliApplication application = new CliApplicationBuilder()
            .AddCommand<RootCommand>()
            .AddCommand<MailCommand>()
            .AddCommand<MusicCommand>()
            .AddCommand<MusicSearchCommand>()
            .AddCommand<CliCompareCommand>()
            .SetExecutableName("playground")
            .SetVersion("0.1.0")
            .Build();

        return await application.RunAsync(normalizedArgs);
    }
}
