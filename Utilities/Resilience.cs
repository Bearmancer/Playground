namespace Playground.Utilities;

using Playground.Logging;
using Playground.Services;
using Polly;

// Centralized resiliency operator for all API/network calls.
//
// POLLY vs SEMAPHORE vs DELAY vs RETRY (Task 17):
// ─────────────────────────────────────────────────
// • Polly        – Resilience library with retry, circuit-breaker, timeout, fallback, etc.
//                  Handles transient failures with configurable exponential backoff.
// • SemaphoreSlim– .NET primitive limiting concurrency. Prevents >1 call at a time to respect
//                  free-tier rate limits (Discogs 60/min, MusicBrainz 1/sec).
// • Task.Delay   – Throttle between calls; ensures minimum gap after each request.
// • Retry        – Polly's retry strategy: on failure, wait (exponentially) then re-attempt
//                  up to MaxRetries. After exhaustion, exception propagates (fail fast).
//
// This helper combines all four: semaphore serializes → delay throttles → Polly retries.
//
// HTTP REPLY STORAGE (Task 19):
// ─────────────────────────────
// Responses are typically JSON. To cache:
//   1. Store raw JSON blobs in files named by a stable hash of the request (e.g., SHA256 of URL+params).
//   2. Use atomic writes (write to .tmp, then rename) to prevent corruption on crash.
//   3. On next request, check file existence + optional TTL header before calling API.
//
// NO-SQL FILE ALTERNATIVES (Task 20):
// ────────────────────────────────────
// For personal projects, flat-file JSON (one file per entity or newline-delimited JSON for bulk)
// suffices until:
//   • Index/query needs grow (full-text search, complex joins) → consider SQLite or LiteDB.
//   • Concurrent writers cause contention → use a real DB.
//   • Dataset > RAM or millions of records → migrate to SQLite or DuckDB.
// Lightweight embedded DBs (SQLite, LevelDB, LiteDB) are good stepping-stones.
//
public static class Resilience
{
    public const int MAX_RETRIES = 10;
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(3);
    public static readonly SemaphoreSlim GlobalLock = new(1, 1);
    public static readonly TimeSpan DiscogsThrottle = TimeSpan.FromMilliseconds(1500);
    public static readonly TimeSpan MusicBrainzThrottle = TimeSpan.FromSeconds(2);

    public static Task<T> ExecuteAsync<T>(Func<Task<T>> action, TimeSpan throttle, string source) =>
        ExecuteInternalAsync(action, throttle, source);

    static async Task<T> ExecuteInternalAsync<T>(
        Func<Task<T>> action,
        TimeSpan throttle,
        string source
    )
    {
        await GlobalLock.WaitAsync();
        try
        {
            if (throttle > TimeSpan.Zero)
                await Task.Delay(throttle);

            ResiliencePipeline pipeline = RetryPolicies.CreateDefault(
                maxRetries: MAX_RETRIES,
                initialDelay: BaseDelay,
                onRetry: (attempt, delay, message) =>
                    SpectreLogger.Warning(
                        $"[{source}] Retry {attempt}/{MAX_RETRIES} in {delay.TotalSeconds:F1}s: {message}"
                    )
            );

            return await pipeline.ExecuteAsync(async _ => await action(), CancellationToken.None);
        }
        finally
        {
            GlobalLock.Release();
        }
    }
}
