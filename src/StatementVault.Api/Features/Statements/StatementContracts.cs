namespace StatementVault.Api.Features.Statements;

public sealed class UploadStatementRequest
{
    public string AccountId { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public IFormFile? File { get; set; }
}

public sealed record StatementResponse(
    string AccountId,
    string StatementId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string ContentType,
    long SizeBytes,
    string S3Key,
    DateTimeOffset CreatedAtUtc,
    string CorrelationId);

public sealed record StatementListResponse(
    IReadOnlyList<StatementResponse> Items,
    string? NextToken);

public sealed record PresignedContentResponse(Uri Url, DateTimeOffset ExpiresAtUtc);
