using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StatementVault.Domain;

namespace StatementVault.Persistence;

/// <summary>
/// Local/dev only: creates the S3 bucket and DynamoDB table when Aws:CreateResources is true.
/// Production Terraform owns these resources.
/// </summary>
public sealed class LocalAwsResourceBootstrapper : IHostedService
{
    private readonly IAmazonS3 _s3;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly AwsOptions _options;
    private readonly ILogger<LocalAwsResourceBootstrapper> _logger;

    public LocalAwsResourceBootstrapper(
        IAmazonS3 s3,
        IAmazonDynamoDB dynamoDb,
        IOptions<AwsOptions> options,
        ILogger<LocalAwsResourceBootstrapper> logger)
    {
        _s3 = s3;
        _dynamoDb = dynamoDb;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.CreateResources)
        {
            return;
        }

        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await EnsureBucketAsync(cancellationToken);
                await EnsureTableAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "AWS emulator not ready (attempt {Attempt}/{MaxAttempts}). Retrying.",
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _s3.GetBucketLocationAsync(_options.BucketName, cancellationToken);
            return;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // create below
        }

        await _s3.PutBucketAsync(new PutBucketRequest
        {
            BucketName = _options.BucketName,
            UseClientRegion = true
        }, cancellationToken);

        await _s3.PutBucketEncryptionAsync(new PutBucketEncryptionRequest
        {
            BucketName = _options.BucketName,
            ServerSideEncryptionConfiguration = new ServerSideEncryptionConfiguration
            {
                ServerSideEncryptionRules =
                [
                    new ServerSideEncryptionRule
                    {
                        ServerSideEncryptionByDefault = new ServerSideEncryptionByDefault
                        {
                            ServerSideEncryptionAlgorithm = ServerSideEncryptionMethod.AES256
                        }
                    }
                ]
            }
        }, cancellationToken);

        _logger.LogInformation("Created S3 bucket {BucketName}.", _options.BucketName);
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _dynamoDb.DescribeTableAsync(_options.TableName, cancellationToken);
            if (existing.Table.TableStatus == TableStatus.ACTIVE)
            {
                return;
            }
        }
        catch (ResourceNotFoundException)
        {
            await _dynamoDb.CreateTableAsync(new CreateTableRequest
            {
                TableName = _options.TableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                AttributeDefinitions =
                [
                    new AttributeDefinition("PK", ScalarAttributeType.S),
                    new AttributeDefinition("SK", ScalarAttributeType.S)
                ],
                KeySchema =
                [
                    new KeySchemaElement("PK", KeyType.HASH),
                    new KeySchemaElement("SK", KeyType.RANGE)
                ]
            }, cancellationToken);

            _logger.LogInformation(
                "Created DynamoDB table {TableName} with PK {Pk} / SK {Sk}.",
                _options.TableName,
                StatementKeys.AccountPrefix,
                StatementKeys.StatementPrefix);
        }

        await WaitForTableAsync(cancellationToken);
    }

    private async Task WaitForTableAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            var table = await _dynamoDb.DescribeTableAsync(_options.TableName, cancellationToken);
            if (table.Table.TableStatus == TableStatus.ACTIVE)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }
}
