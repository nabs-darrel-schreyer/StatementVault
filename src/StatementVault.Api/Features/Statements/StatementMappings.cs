using StatementVault.Domain;

namespace StatementVault.Api.Features.Statements;

internal static class StatementMappings
{
    public static StatementResponse ToResponse(this Statement statement) =>
        new(
            statement.AccountId,
            statement.StatementId,
            statement.PeriodStart,
            statement.PeriodEnd,
            statement.ContentType,
            statement.SizeBytes,
            statement.S3Key,
            statement.CreatedAtUtc,
            statement.CorrelationId);
}
