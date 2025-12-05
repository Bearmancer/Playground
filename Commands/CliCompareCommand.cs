namespace Playground.Commands;

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Playground.Cli;
using Playground.Logging;

[Command(
    "cli compare",
    Description = "Execute a comparative benchmark of available CLI framework runners."
)]
public sealed class CliCompareCommand : ICommand
{
    public async ValueTask ExecuteAsync(IConsole console)
    {
        SpectreLogger.Rule("CLI Framework Comparison");

        ICliRunner[] runners = CliComparison.GetAllRunners();
        string[] testArgs = ["scrape", "--verbose"];

        foreach (ICliRunner runner in runners)
        {
            SpectreLogger.Starting($"Testing: {runner.Name}");
            int result = await runner.RunAsync(testArgs);
            string status = result == 0 ? "Success" : "Failed";
            SpectreLogger.KeyValue(runner.Name, status);
        }
    }
}
