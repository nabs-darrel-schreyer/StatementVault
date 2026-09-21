using Amazon.CDK;
using StatementVault.Infra;

var outdir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "cdk.out"));
var app = new App(new AppProps { Outdir = outdir });

var envName = Context(app, "env", "dev");
var project = Context(app, "project", "statementvault");
var allowedCidr = Context(app, "allowedCidr", "0.0.0.0/0");
var instanceType = Context(app, "instanceType", "t3.micro");
var apiImage = Context(app, "apiImage", string.Empty);
var apiPort = int.TryParse(app.Node.TryGetContext("apiPort")?.ToString(), out var parsedPort)
    ? parsedPort
    : 8080;

_ = new StatementVaultStack(app, $"StatementVault-{envName}", new StatementVaultStackProps
{
    Project = project,
    EnvName = envName,
    AllowedCidr = allowedCidr,
    InstanceTypeName = instanceType,
    ApiPort = apiPort,
    ApiImage = string.IsNullOrWhiteSpace(apiImage) ? null : apiImage,
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1"
    }
});

app.Synth();
Console.WriteLine($"Synthesized CloudFormation to {outdir}");

static string Context(App app, string key, string fallback)
{
    var value = app.Node.TryGetContext(key)?.ToString();
    return string.IsNullOrWhiteSpace(value) ? fallback : value;
}
