namespace Playground.Models;

public record RetryConfig(
    int MaxRetries = 5,
    int InitialDelayMs = 1000,
    double BackoffMultiplier = 2.0
);

public record MusicSearchResult(
    string Title,
    string Artist,
    int? Year,
    string Source,
    string ExternalId
);

public record SongEntry(string Name, string Year, string Url);

public record CachedPage(string Content, DateTime CachedAt);

public record RegexConfig(string[] Patterns, string[] Exclusions);

public record MailAccount(string Id, string Address, string Password);

public record MailMessage(
    string Id,
    MailAddress? From,
    MailAddress? To,
    string Subject,
    string? Text,
    DateTime CreatedAt
);

public record MailAddress(string Address, string? Name);
