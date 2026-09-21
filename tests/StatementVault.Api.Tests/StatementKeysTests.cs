using StatementVault.Domain;

namespace StatementVault.Api.Tests;

public sealed class StatementKeysTests
{
    [Fact]
    public void PartitionAndSortKeys_UseDocumentedPrefixes()
    {
        Assert.Equal("ACCOUNT#acc-1", StatementKeys.PartitionKey("acc-1"));
        Assert.Equal("STATEMENT#stmt-1", StatementKeys.SortKey("stmt-1"));
        Assert.Equal("acc-1", StatementKeys.AccountIdFromPartitionKey("ACCOUNT#acc-1"));
        Assert.Equal("stmt-1", StatementKeys.StatementIdFromSortKey("STATEMENT#stmt-1"));
    }

    [Theory]
    [InlineData("jan.pdf", "statements/acc-1/abc.pdf")]
    [InlineData(".PDF", "statements/acc-1/abc.pdf")]
    [InlineData(null, "statements/acc-1/abc")]
    public void ObjectKey_IncludesAccountStatementAndExtension(string? extension, string expected)
    {
        Assert.Equal(expected, StatementKeys.ObjectKey("acc-1", "abc", extension));
    }
}
