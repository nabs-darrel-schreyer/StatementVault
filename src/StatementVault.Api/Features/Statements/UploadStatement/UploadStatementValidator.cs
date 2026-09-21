using FluentValidation;

namespace StatementVault.Api.Features.Statements.UploadStatement;

public sealed class UploadStatementValidator : AbstractValidator<UploadStatementRequest>
{
    public const long MaxFileBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "text/csv",
        "text/plain",
        "application/xml",
        "text/xml",
        "application/octet-stream"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".csv",
        ".txt",
        ".ofx",
        ".qfx",
        ".xml"
    };

    public UploadStatementValidator()
    {
        RuleFor(x => x.AccountId)
            .Must(StatementIdRules.IsValid)
            .WithMessage("AccountId must be 1-64 characters of letters, digits, '.', '_' or '-'.");

        RuleFor(x => x.PeriodStart)
            .NotEqual(default(DateOnly))
            .WithMessage("PeriodStart is required.");

        RuleFor(x => x.PeriodEnd)
            .NotEqual(default(DateOnly))
            .WithMessage("PeriodEnd is required.")
            .GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("PeriodEnd must be on or after PeriodStart.");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("A statement file is required.");

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!)
                .Must(file => file.Length > 0)
                .WithMessage("The statement file is empty.")
                .Must(file => file.Length <= MaxFileBytes)
                .WithMessage($"The statement file must be {MaxFileBytes} bytes or smaller.")
                .Must(file => AllowedContentTypes.Contains(file.ContentType) || HasAllowedExtension(file.FileName))
                .WithMessage("Unsupported statement file type. Use PDF, CSV, TXT, OFX, QFX, or XML.")
                .Must(file => HasAllowedExtension(file.FileName))
                .WithMessage("Unsupported statement file extension.");
        });
    }

    private static bool HasAllowedExtension(string? fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName ?? string.Empty));
}
