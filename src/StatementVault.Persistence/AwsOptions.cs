using System.ComponentModel.DataAnnotations;

namespace StatementVault.Persistence;

public sealed class AwsOptions
{
    public const string SectionName = "Aws";

    [Required]
    public string Region { get; set; } = "us-east-1";

    [Required]
    public string BucketName { get; set; } = "statement-vault";

    [Required]
    public string TableName { get; set; } = "Statements";

    /// <summary>
    /// LocalStack (or other emulator) endpoint. Leave empty on EC2 / real AWS so the
    /// SDK uses the default regional endpoints.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Required for LocalStack S3. Leave false against real AWS.
    /// </summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// When true (local/dev), create the S3 bucket and DynamoDB table if they are missing.
    /// Never enable this on a locked-down production account.
    /// </summary>
    public bool CreateResources { get; set; }

    public int PresignedUrlLifetimeMinutes { get; set; } = 15;
}
