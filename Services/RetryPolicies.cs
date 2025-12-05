namespace Playground.Services;

// Config record for Polly/retry helpers
public record RetryConfig(
    int MaxRetries = 10,
    int InitialDelayMs = 3000,
    double BackoffMultiplier = 2.0
);

public static class RetryPolicies
{
    const int DEFAULT_MAX_RETRIES = 10;
    static readonly TimeSpan DEFAULT_INITIAL_DELAY = TimeSpan.FromSeconds(3);

    public static ResiliencePipeline CreateDefault(
        int maxRetries = DEFAULT_MAX_RETRIES,
        TimeSpan? initialDelay = null,
        Action<int, TimeSpan, string?>? onRetry = null
    )
    {
        TimeSpan delay = initialDelay ?? DEFAULT_INITIAL_DELAY;

        return new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = maxRetries,
                    Delay = delay,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    OnRetry = args =>
                    {
                        string? message = args.Outcome.Exception?.Message;
                        if (onRetry is not null)
                            onRetry(args.AttemptNumber, args.RetryDelay, message);
                        else
                        {
                            SpectreLogger.Warning(
                                $"Retry {args.AttemptNumber}/{maxRetries} after {args.RetryDelay.TotalSeconds:F1}s - {message}"
                            );
                        }
                        return ValueTask.CompletedTask;
                    },
                }
            )
            .Build();
    }

    public static ResiliencePipeline CreatePipeline(RetryConfig? config = null)
    {
        config ??= new RetryConfig();
        return CreateDefault(
            maxRetries: config.MaxRetries,
            initialDelay: TimeSpan.FromMilliseconds(config.InitialDelayMs)
        );
    }

    public static ResiliencePipeline<HttpResponseMessage> CreateHttpPipeline(
        RetryConfig? config = null
    )
    {
        config ??= new RetryConfig();

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(
                new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = config.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(config.InitialDelayMs),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(r => !r.IsSuccessStatusCode),
                    OnRetry = args =>
                    {
                        SpectreLogger.Warning(
                            $"HTTP retry {args.AttemptNumber}/{config.MaxRetries} - {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}"
                        );
                        return ValueTask.CompletedTask;
                    },
                }
            )
            .Build();
    }

    public static Polly.IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(
        RetryConfig? config = null
    )
    {
        config ??= new RetryConfig();

        return Polly
            .Policy.Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                config.MaxRetries,
                retryAttempt =>
                    TimeSpan.FromMilliseconds(
                        config.InitialDelayMs * Math.Pow(config.BackoffMultiplier, retryAttempt - 1)
                    ),
                (outcome, timespan, retryCount, _) =>
                {
                    SpectreLogger.Warning(
                        $"Polly retry {retryCount}/{config.MaxRetries} after {timespan.TotalSeconds:F1}s - {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}"
                    );
                }
            );
    }

    public static ResiliencePipeline CreateForHttpClient(int maxRetries = DEFAULT_MAX_RETRIES)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = maxRetries,
                    Delay = DEFAULT_INITIAL_DELAY,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>(),
                    OnRetry = args =>
                    {
                        SpectreLogger.Warning(
                            $"HTTP retry {args.AttemptNumber}/{maxRetries} - {args.Outcome.Exception?.Message}"
                        );
                        return ValueTask.CompletedTask;
                    },
                }
            )
            .Build();
    }
}
