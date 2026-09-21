using Microsoft.Extensions.Options;
using StatementVault.Domain;
using StatementVault.Persistence;

namespace StatementVault.Api.Features.Statements.DownloadStatement;

public static class DownloadStatementEndpoint
{
    public static RouteGroupBuilder MapDownloadStatement(this RouteGroupBuilder group)
    {
        group.MapGet("/{accountId}/{statementId}/content", HandleAsync)
            .WithName("DownloadStatement")
            .WithSummary("Download statement bytes or return a short-lived S3 presigned URL.")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .Produces<PresignedContentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string accountId,
        string statementId,
        string? mode,
        IStatementRepository repository,
        IStatementObjectStore objectStore,
        IOptions<AwsOptions> awsOptions,
        CancellationToken cancellationToken)
    {
        if (!StatementIdRules.IsValid(accountId) || !StatementIdRules.IsValid(statementId))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid accountId or statementId.");
        }

        var statement = await repository.GetAsync(accountId, statementId, cancellationToken);
        if (statement is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Statement not found.");
        }

        if (string.Equals(mode, "url", StringComparison.OrdinalIgnoreCase))
        {
            var lifetime = TimeSpan.FromMinutes(Math.Clamp(awsOptions.Value.PresignedUrlLifetimeMinutes, 1, 60));
            var url = await objectStore.CreatePresignedGetUrlAsync(statement.S3Key, lifetime, cancellationToken);
            return TypedResults.Ok(new PresignedContentResponse(url, DateTimeOffset.UtcNow.Add(lifetime)));
        }

        var blob = await objectStore.GetAsync(statement.S3Key, cancellationToken);
        if (blob is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Statement file was not found in object storage.");
        }

        var fileName = statement.FileName ?? Path.GetFileName(statement.S3Key);
        return TypedResults.File(blob.Content, blob.ContentType, fileName);
    }
}
