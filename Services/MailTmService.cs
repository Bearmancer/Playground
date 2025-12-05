using Playground.Logging;
using Polly;
using Polly.Retry;
using RestSharp;

namespace Playground.Services;

public class MailTmException(string message, Exception? inner = null) : Exception(message, inner);

public class MailTmService
{
    readonly RestClient Client;
    readonly ResiliencePipeline RetryPipeline;
    string? AuthToken;
    string? CurrentAccountId;
    string? CurrentPassword;
    const string BaseUrl = "https://api.mail.tm";
    const int MaxRetries = 5;
    static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(1);

    public MailTmService()
    {
        Client = new RestClient(BaseUrl);

        RetryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = MaxRetries,
                    Delay = InitialDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    OnRetry = args =>
                    {
                        SpectreLogger.Warning(
                            $"Retry {args.AttemptNumber}/{MaxRetries} after {args.RetryDelay.TotalSeconds:F1}s"
                        );
                        return ValueTask.CompletedTask;
                    },
                }
            )
            .Build();

        SpectreLogger.Info("MailTmService initialized with exponential retry");
    }

    public async Task<MailTmAccount> CreateAccountAsync()
    {
        SpectreLogger.Starting("Creating mail.tm account");

        string domain = await GetAvailableDomainAsync();
        string username = $"test_{DateTime.UtcNow.Ticks}";
        string address = $"{username}@{domain}";
        string password = GenerateSecurePassword();

        return await RetryPipeline.ExecuteAsync(async _ =>
        {
            RestRequest request = new("/accounts", Method.Post);
            request.AddJsonBody(new { address, password });

            RestResponse<MailTmAccount> response = await Client.ExecuteAsync<MailTmAccount>(request);

            if (!response.IsSuccessful || response.Data is null)
                throw new MailTmException($"Failed to create account: {response.StatusCode} - {response.Content}");

            CurrentAccountId = response.Data.Id;
            CurrentPassword = password;

            await AuthenticateAsync(address, password);

            SpectreLogger.Complete($"Account created: {address}");
            SpectreLogger.KeyValue("Account ID", response.Data.Id);

            return response.Data;
        });
    }

    async Task<string> GetAvailableDomainAsync()
    {
        RestRequest request = new("/domains", Method.Get);
        RestResponse response = await Client.ExecuteAsync(request);

        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            throw new MailTmException($"Failed to get domains: {response.StatusCode}");

        using JsonDocument doc = JsonDocument.Parse(response.Content);
        JsonElement root = doc.RootElement;

        JsonElement domains =
            root.ValueKind == JsonValueKind.Array ? root
            : root.TryGetProperty("hydra:member", out JsonElement members) ? members
            : root;

        if (domains.ValueKind == JsonValueKind.Array && domains.GetArrayLength() > 0)
        {
            string? domain = domains[0].GetProperty("domain").GetString();
            if (!string.IsNullOrEmpty(domain))
                return domain;
        }

        throw new MailTmException("No available domains found");
    }

    async Task AuthenticateAsync(string address, string password)
    {
        SpectreLogger.Debug($"Authenticating: {address}");

        RestRequest request = new("/token", Method.Post);
        request.AddJsonBody(new { address, password });

        RestResponse<TokenResponse> response = await Client.ExecuteAsync<TokenResponse>(request);

        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Data?.Token))
            throw new MailTmException($"Authentication failed: {response.StatusCode}");

        AuthToken = response.Data.Token;
        SpectreLogger.Debug("Authentication successful");
    }

    public async Task<List<MailTmMessage>> GetInboxAsync()
    {
        if (string.IsNullOrEmpty(AuthToken))
            throw new MailTmException("Not authenticated. Call CreateAccountAsync first.");

        SpectreLogger.Starting("Fetching inbox");

        return await RetryPipeline.ExecuteAsync(async _ =>
        {
            RestRequest request = new("/messages", Method.Get);
            request.AddHeader("Authorization", $"Bearer {AuthToken}");

            RestResponse response = await Client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                throw new MailTmException($"Failed to fetch inbox: {response.StatusCode}");

            using JsonDocument doc = JsonDocument.Parse(response.Content);
            JsonElement root = doc.RootElement;
            List<MailTmMessage> messages = [];

            JsonElement messageArray =
                root.ValueKind == JsonValueKind.Array ? root
                : root.TryGetProperty("hydra:member", out JsonElement members) ? members
                : throw new MailTmException("Unexpected inbox response format");

            foreach (JsonElement elem in messageArray.EnumerateArray())
            {
                messages.Add(ParseMessage(elem));
            }

            SpectreLogger.Complete($"Found {messages.Count} messages");
            return messages;
        });
    }

    public async Task<MailTmMessage> ReadMessageAsync(string messageId)
    {
        if (string.IsNullOrEmpty(AuthToken))
            throw new MailTmException("Not authenticated. Call CreateAccountAsync first.");

        SpectreLogger.Starting($"Reading message: {messageId}");

        return await RetryPipeline.ExecuteAsync(async _ =>
        {
            RestRequest request = new($"/messages/{messageId}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {AuthToken}");

            RestResponse<MailTmMessage> response = await Client.ExecuteAsync<MailTmMessage>(request);

            if (!response.IsSuccessful || response.Data is null)
                throw new MailTmException($"Failed to read message: {response.StatusCode}");

            SpectreLogger.Complete("Message loaded");
            return response.Data;
        });
    }

    public async Task<bool> DeleteAccountAsync()
    {
        if (string.IsNullOrEmpty(AuthToken) || string.IsNullOrEmpty(CurrentAccountId))
            throw new MailTmException("Not authenticated. Call CreateAccountAsync first.");

        SpectreLogger.Starting("Deleting account");

        return await RetryPipeline.ExecuteAsync(async _ =>
        {
            RestRequest request = new($"/accounts/{CurrentAccountId}", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {AuthToken}");

            RestResponse response = await Client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                throw new MailTmException($"Failed to delete account: {response.StatusCode}");

            AuthToken = null;
            CurrentAccountId = null;
            CurrentPassword = null;

            SpectreLogger.Complete("Account deleted");
            return true;
        });
    }

    static MailTmMessage ParseMessage(JsonElement elem) =>
        new()
        {
            Id = elem.GetProperty("id").GetString() ?? "",
            AccountId = elem.TryGetProperty("accountId", out var aid) ? aid.GetString() ?? "" : "",
            Subject = elem.TryGetProperty("subject", out var subj) ? subj.GetString() ?? "" : "",
            From = elem.TryGetProperty("from", out var from)
                ? new MailTmAddress
                {
                    Address = from.GetProperty("address").GetString() ?? "",
                    Name = from.TryGetProperty("name", out var n) ? n.GetString() : null,
                }
                : null,
            CreatedAt =
                elem.TryGetProperty("createdAt", out var ca)
                && DateTime.TryParse(ca.GetString(), out var dt)
                    ? dt
                    : DateTime.MinValue,
        };

    static string GenerateSecurePassword(int length = 20)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return new string(
            Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray()
        );
    }
}

public record MailTmAccount
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("quota")]
    public int Quota { get; init; }

    [JsonPropertyName("used")]
    public int Used { get; init; }

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public record TokenResponse
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

public record MailTmAddress
{
    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public record MailTmMessage
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("accountId")]
    public required string AccountId { get; init; }

    [JsonPropertyName("msgid")]
    public string? MsgId { get; init; }

    [JsonPropertyName("from")]
    public MailTmAddress? From { get; init; }

    [JsonPropertyName("to")]
    public MailTmAddress[]? To { get; init; }

    [JsonPropertyName("cc")]
    public MailTmAddress[]? Cc { get; init; }

    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("html")]
    public string? Html { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }
}
