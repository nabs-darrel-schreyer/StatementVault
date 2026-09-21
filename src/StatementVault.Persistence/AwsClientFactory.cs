using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace StatementVault.Persistence;

internal static class AwsClientFactory
{
    public static IAmazonS3 CreateS3(IOptions<AwsOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
        };

        ApplyEndpoint(config, options);
        config.ForcePathStyle = options.ForcePathStyle || HasServiceUrl(options);

        // Default credential chain (env, profile, EC2 instance profile). No static keys.
        return new AmazonS3Client(config);
    }

    public static IAmazonDynamoDB CreateDynamoDb(IOptions<AwsOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
        };

        ApplyEndpoint(config, options);
        return new AmazonDynamoDBClient(config);
    }

    private static void ApplyEndpoint(ClientConfig config, AwsOptions options)
    {
        if (!HasServiceUrl(options))
        {
            return;
        }

        config.ServiceURL = options.ServiceUrl;
        config.AuthenticationRegion = options.Region;
    }

    private static bool HasServiceUrl(AwsOptions options) =>
        !string.IsNullOrWhiteSpace(options.ServiceUrl);
}
