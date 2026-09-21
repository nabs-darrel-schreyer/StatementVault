var builder = DistributedApplication.CreateBuilder(args);

var awsRegion = builder.Configuration["Aws:Region"] ?? "us-east-1";
var bucketName = builder.Configuration["Aws:BucketName"] ?? "statement-vault";
var tableName = builder.Configuration["Aws:TableName"] ?? "Statements";
var useLocalStack = !string.Equals(builder.Configuration["Aws:UseLocalStack"], "false", StringComparison.OrdinalIgnoreCase);

var api = builder.AddProject<Projects.StatementVault_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Aws__Region", awsRegion)
    .WithEnvironment("Aws__BucketName", bucketName)
    .WithEnvironment("Aws__TableName", tableName)
    .WithEnvironment("AWS_DEFAULT_REGION", awsRegion);

if (useLocalStack)
{
    var localstack = builder.AddContainer("localstack", "localstack/localstack", "4.8")
        .WithHttpEndpoint(targetPort: 4566, name: "edge")
        .WithEnvironment("SERVICES", "s3,dynamodb")
        .WithEnvironment("DEFAULT_REGION", awsRegion)
        .WithEnvironment("EAGER_SERVICE_LOADING", "1")
        .WithHttpHealthCheck("/_localstack/health", endpointName: "edge");

    api.WithEnvironment("Aws__ServiceUrl", localstack.GetEndpoint("edge"))
        .WithEnvironment("Aws__ForcePathStyle", "true")
        .WithEnvironment("Aws__CreateResources", "true")
        .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
        .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
        .WaitFor(localstack);
}

builder.Build().Run();
