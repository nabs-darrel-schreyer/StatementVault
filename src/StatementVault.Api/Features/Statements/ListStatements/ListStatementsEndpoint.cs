using StatementVault.Domain;

namespace StatementVault.Api.Features.Statements.ListStatements;

public static class ListStatementsEndpoint
{
    public static RouteGroupBuilder MapListStatements(this RouteGroupBuilder group)
    {
        group.MapGet("/{accountId}", HandleAsync)
            .WithName("ListStatements")
            .WithSummary("List statements for an account (DynamoDB query, paginated).")
            .Produces<StatementListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string accountId,
        IStatementRepository repository,
        int? limit,
        string? nextToken,
        CancellationToken cancellationToken)
    {
        if (!StatementIdRules.IsValid(accountId))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid accountId.");
        }

        var pageSize = limit switch
        {
            null => 25,
            < 1 or > 100 => 0,
            _ => limit.Value
        };

        if (pageSize == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "limit must be between 1 and 100.");
        }

        var page = await repository.QueryByAccountAsync(accountId, pageSize, nextToken, cancellationToken);
        var response = new StatementListResponse(page.Items.Select(item => item.ToResponse()).ToArray(), page.NextToken);
        return TypedResults.Ok(response);
    }
}
