namespace Playground.Cli;

public interface ICliRunner
{
    string Name { get; }
    Task<int> RunAsync(string[] args);
}

public record CliOptions(
    bool Verbose = false,
    string? Output = null,
    string? Query = null,
    int Timeout = 30
);
