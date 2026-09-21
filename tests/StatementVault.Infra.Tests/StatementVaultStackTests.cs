using Amazon.CDK;
using Amazon.CDK.Assertions;
using StatementVault.Infra;

namespace StatementVault.Infra.Tests;

public sealed class StatementVaultStackTests
{
    [Fact]
    public void Bucket_Encrypts_AndBlocksPublicAccess()
    {
        var template = Synthesize();

        template.HasResourceProperties("AWS::S3::Bucket", Match.ObjectLike(new Dictionary<string, object>
        {
            ["BucketEncryption"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["ServerSideEncryptionConfiguration"] = Match.ArrayWith(new object[]
                {
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["ServerSideEncryptionByDefault"] = Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["SSEAlgorithm"] = "AES256"
                        })
                    })
                })
            }),
            ["PublicAccessBlockConfiguration"] = new Dictionary<string, object>
            {
                ["BlockPublicAcls"] = true,
                ["BlockPublicPolicy"] = true,
                ["IgnorePublicAcls"] = true,
                ["RestrictPublicBuckets"] = true
            },
            ["OwnershipControls"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["Rules"] = Match.ArrayWith(new object[]
                {
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["ObjectOwnership"] = "BucketOwnerEnforced"
                    })
                })
            })
        }));
    }

    [Fact]
    public void Table_IsPayPerRequest_WithPkAndSk()
    {
        var template = Synthesize();

        template.HasResourceProperties("AWS::DynamoDB::Table", Match.ObjectLike(new Dictionary<string, object>
        {
            ["BillingMode"] = "PAY_PER_REQUEST",
            ["KeySchema"] = new object[]
            {
                new Dictionary<string, object> { ["AttributeName"] = "PK", ["KeyType"] = "HASH" },
                new Dictionary<string, object> { ["AttributeName"] = "SK", ["KeyType"] = "RANGE" }
            }
        }));
    }

    [Fact]
    public void InstanceRole_HasLeastPrivilegeActions()
    {
        var template = Synthesize();

        template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
        {
            ["PolicyDocument"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["Statement"] = Match.ArrayWith(new object[]
                {
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["Action"] = Match.ArrayWith(new object[] { "s3:GetObject", "s3:PutObject" })
                    }),
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["Action"] = Match.ArrayWith(new object[] { "s3:ListBucket" })
                    }),
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["Action"] = Match.ArrayWith(new object[]
                        {
                            "dynamodb:GetItem",
                            "dynamodb:PutItem",
                            "dynamodb:Query"
                        })
                    })
                })
            })
        }));
    }

    [Fact]
    public void Host_IsEc2_WithImdsv2()
    {
        var template = Synthesize();

        template.ResourceCountIs("AWS::EC2::Instance", 1);
        template.HasResourceProperties("AWS::EC2::LaunchTemplate", Match.ObjectLike(new Dictionary<string, object>
        {
            ["LaunchTemplateData"] = Match.ObjectLike(new Dictionary<string, object>
            {
                ["MetadataOptions"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["HttpTokens"] = "required"
                })
            })
        }));
    }

    private static Template Synthesize()
    {
        var app = new App();
        var stack = new StatementVaultStack(app, "Test", new StatementVaultStackProps
        {
            EnvName = "test",
            Project = "statementvault",
            AllowedCidr = "10.0.0.0/8",
            InstanceTypeName = "t3.micro",
            ApiPort = 8080
        });
        return Template.FromStack(stack);
    }
}
