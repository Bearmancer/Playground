namespace Playground.Cli;

public static class CliComparison
{
    public static ICliRunner[] GetAllRunners() =>
        [
            new CommandLineParserRunner(),
            new SpectreCliRunner(),
            new SystemCommandLineRunner(),
            new CoconaRunner(),
            new CliFxRunner(),
        ];

    public static async Task RunAllAsync(string[] args)
    {
        SpectreLogger.Rule("CLI Implementation Comparison");

        foreach (ICliRunner runner in GetAllRunners())
        {
            SpectreLogger.Info($"Running {runner.Name}...");
            try
            {
                int result = await runner.RunAsync(args);
                SpectreLogger.KeyValue($"{runner.Name} Exit Code", result.ToString());
            }
            catch (Exception ex)
            {
                SpectreLogger.Error($"{runner.Name} failed: {ex.Message}");
            }
        }
    }

    public static async Task DemoAsync()
    {
        SpectreLogger.Rule("CLI Demo - Same Commands Across All Implementations");

        string[][] testCases =
        [
            ["scrape", "--verbose", "--output=output.csv"],
            ["mail", "-v"],
            ["search", "David Bowie", "--timeout=60"],
        ];

        foreach (string[] testCase in testCases)
        {
            SpectreLogger.Info($"Command: {string.Join(" ", testCase)}");
            SpectreLogger.WriteLine("");

            foreach (ICliRunner runner in GetAllRunners())
            {
                try
                {
                    await runner.RunAsync(testCase);
                }
                catch (Exception ex)
                {
                    SpectreLogger.Error($"{runner.Name}: {ex.Message}");
                }
            }

            SpectreLogger.WriteLine("");
        }
    }
}
