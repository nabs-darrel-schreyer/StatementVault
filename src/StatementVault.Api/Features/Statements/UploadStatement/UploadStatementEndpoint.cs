using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StatementVault.Api.Hosting;
using StatementVault.Domain;
using StatementVault.Persistence;

namespace StatementVault.Api.Features.Statements.UploadStatement;

public static class UploadStatementEndpoint
{
    public static RouteGroupBuilder MapUploadStatement(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("UploadStatement")
            .WithSummary("Upload a statement file and persist its metadata.")
            .DisableAntiforgery()
            .Accepts<UploadStatementRequest>("multipart/form-data")
            .Produces<StatementResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<UploadStatementRequest>>();

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromForm] UploadStatementRequest request,
        IStatementRepository repository,
        IStatementObjectStore objectStore,
        IOptions<AwsOptions> awsOptions,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(UploadStatementEndpoint));
        var statementId = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(request.File!.FileName);
        var s3Key = StatementKeys.ObjectKey(request.AccountId, statementId, extension);
        var correlationId = CorrelationIdMiddleware.Get(httpContext);
        var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
            ? "application/octet-stream"
            : request.File.ContentType;

        await using var uploadStream = request.File.OpenReadStream();
        await objectStore.PutAsync(s3Key, uploadStream, contentType, cancellationToken);

        var statement = new Statement
        {
            AccountId = request.AccountId,
            StatementId = statementId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            ContentType = contentType,
            SizeBytes = request.File.Length,
            S3Key = s3Key,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            FileName = request.File.FileName
        };

        await repository.PutAsync(statement, cancellationToken);

        logger.LogInformation(
            "Stored statement {StatementId} for account {AccountId} in bucket {BucketName} key {S3Key}.",
            statementId,
            request.AccountId,
            awsOptions.Value.BucketName,
            s3Key);

        return TypedResults.Created(
            $"/api/statements/{statement.AccountId}/{statement.StatementId}",
            statement.ToResponse());
    }
}
