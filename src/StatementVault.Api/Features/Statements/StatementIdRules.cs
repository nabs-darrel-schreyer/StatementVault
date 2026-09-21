using System.Text.RegularExpressions;

namespace StatementVault.Api.Features.Statements;

internal static partial class StatementIdRules
{
    public const int MaxLength = 64;

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxLength
        && IdentifierRegex().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
