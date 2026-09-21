using StatementVault.Api.Features.Statements.DownloadStatement;
using StatementVault.Api.Features.Statements.GetStatement;
using StatementVault.Api.Features.Statements.ListStatements;
using StatementVault.Api.Features.Statements.UploadStatement;

namespace StatementVault.Api.Features.Statements;

public static class StatementEndpoints
{
    public static IEndpointRouteBuilder MapStatementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/statements")
            .WithTags("Statements");

        group.MapUploadStatement();
        group.MapListStatements();
        group.MapGetStatement();
        group.MapDownloadStatement();

        return app;
    }
}
