namespace StatementVault.Domain;

public sealed record Statement
{
    public required string AccountId { get; init; }
    public required string StatementId { get; init; }
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string S3Key { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required string CorrelationId { get; init; }
    public string? FileName { get; init; }
}
