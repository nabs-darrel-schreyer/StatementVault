using StatementVault.Domain;

namespace StatementVault.Api.Features.Statements.GetStatement;

public static class GetStatementEndpoint
{
    public static RouteGroupBuilder MapGetStatement(this RouteGroupBuilder group)
    {
        group.MapGet("/{accountId}/{statementId}", HandleAsync)
            .WithName("GetStatement")
            .WithSummary("Get statement metadata from DynamoDB.")
            .Produces<StatementResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string accountId,
        string statementId,
        IStatementRepository repository,
        CancellationToken cancellationToken)
    {
        if (!StatementIdRules.IsValid(accountId) || !StatementIdRules.IsValid(statementId))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid accountId or statementId.");
        }

        var statement = await repository.GetAsync(accountId, statementId, cancellationToken);
        return statement is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Statement not found.")
            : TypedResults.Ok(statement.ToResponse());
    }
}
