using Microsoft.AspNetCore.Http;
using StatementVault.Api.Features.Statements;
using StatementVault.Api.Features.Statements.UploadStatement;

namespace StatementVault.Api.Tests;

public sealed class UploadStatementValidatorTests
{
    private readonly UploadStatementValidator _validator = new();

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(CreateRequest(), TestContext.Current.CancellationToken);
        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public async Task EndBeforeStart_Fails()
    {
        var request = CreateRequest();
        request.PeriodEnd = new DateOnly(2025, 12, 31);

        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UploadStatementRequest.PeriodEnd));
    }

    private static UploadStatementRequest CreateRequest()
    {
        var file = new FormFile(new MemoryStream("pdf"u8.ToArray()), 0, 3, "file", "jan.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        return new UploadStatementRequest
        {
            AccountId = "acc-1001",
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            File = file
        };
    }
}
