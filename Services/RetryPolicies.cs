using Playground.Logging;
using Playground.Models;
using Polly;
using Polly.Retry;

namespace Playground.Services;

public static class RetryPolicies
{
    public static ResiliencePipeline CreateDefault(
        int maxRetries = 5,
        TimeSpan? initialDelay = null,
        Action<int, TimeSpan, string?>? onRetry = null)
    {
        TimeSpan delay = initialDelay ?? TimeSpan.FromSeconds(1);

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
                        onRetry?.Invoke(args.AttemptNumber, args.RetryDelay, message);
                        SpectreLogger.Warning(
                            $"Retry {args.AttemptNumber}/{maxRetries} after {args.RetryDelay.TotalSeconds:F1}s - {message}"
                        );
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

    public static ResiliencePipeline<HttpResponseMessage> CreateHttpPipeline(RetryConfig? config = null)
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

    public static Polly.IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(RetryConfig? config = null)
    {
        config ??= new RetryConfig();

        return Polly.Policy
            .Handle<HttpRequestException>()
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

    public static ResiliencePipeline CreateForHttpClient(int maxRetries = 3)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = maxRetries,
                    Delay = TimeSpan.FromSeconds(2),
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
