using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StatementVault.Domain;

namespace StatementVault.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStatementVaultPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddOptions<AwsOptions>()
            .Bind(configuration.GetSection(AwsOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.BucketName) && !string.IsNullOrWhiteSpace(options.TableName),
                "Aws:BucketName and Aws:TableName are required.")
            .ValidateOnStart();

        if (environment.IsEnvironment("Testing"))
        {
            return services;
        }

        services.AddSingleton<IAmazonS3>(sp =>
            AwsClientFactory.CreateS3(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AwsOptions>>()));
        services.AddSingleton<IAmazonDynamoDB>(sp =>
            AwsClientFactory.CreateDynamoDb(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AwsOptions>>()));
        services.AddSingleton<IStatementRepository, DynamoDbStatementRepository>();
        services.AddSingleton<IStatementObjectStore, S3StatementObjectStore>();
        services.AddHostedService<LocalAwsResourceBootstrapper>();

        return services;
    }
}
