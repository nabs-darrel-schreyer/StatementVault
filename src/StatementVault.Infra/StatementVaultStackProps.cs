namespace StatementVault.Infra;

public sealed class StatementVaultStackProps : Amazon.CDK.StackProps
{
    public string Project { get; init; } = "statementvault";

    public string EnvName { get; init; } = "dev";

    public string AllowedCidr { get; init; } = "0.0.0.0/0";

    public string InstanceTypeName { get; init; } = "t3.micro";

    public int ApiPort { get; init; } = 8080;

    /// <summary>
    /// Optional container image. When empty, user_data installs Docker and writes publish instructions.
    /// </summary>
    public string? ApiImage { get; init; }
}
