namespace StatementVault.Domain;

public static class StatementKeys
{
    public const string AccountPrefix = "ACCOUNT#";
    public const string StatementPrefix = "STATEMENT#";

    public static string PartitionKey(string accountId) => $"{AccountPrefix}{accountId}";

    public static string SortKey(string statementId) => $"{StatementPrefix}{statementId}";

    public static string ObjectKey(string accountId, string statementId, string? extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(statementId);

        var ext = NormalizeExtension(extension);
        return $"statements/{accountId}/{statementId}{ext}";
    }

    public static string AccountIdFromPartitionKey(string partitionKey)
    {
        if (!partitionKey.StartsWith(AccountPrefix, StringComparison.Ordinal))
        {
            throw new FormatException($"Partition key '{partitionKey}' does not start with {AccountPrefix}.");
        }

        return partitionKey[AccountPrefix.Length..];
    }

    public static string StatementIdFromSortKey(string sortKey)
    {
        if (!sortKey.StartsWith(StatementPrefix, StringComparison.Ordinal))
        {
            throw new FormatException($"Sort key '{sortKey}' does not start with {StatementPrefix}.");
        }

        return sortKey[StatementPrefix.Length..];
    }

    public static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        return extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
    }
}
