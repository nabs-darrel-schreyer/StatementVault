namespace StatementVault.Domain;

public sealed record StatementPage(IReadOnlyList<Statement> Items, string? NextToken);
