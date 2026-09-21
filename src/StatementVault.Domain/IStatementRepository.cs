namespace StatementVault.Domain;

public interface IStatementRepository
{
    Task PutAsync(Statement statement, CancellationToken cancellationToken);

    Task<Statement?> GetAsync(string accountId, string statementId, CancellationToken cancellationToken);

    Task<StatementPage> QueryByAccountAsync(
        string accountId,
        int limit,
        string? paginationToken,
        CancellationToken cancellationToken);
}
