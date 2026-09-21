using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace StatementVault.Infra;

public sealed class StatementVaultStack : Stack
{
    public IBucket StatementsBucket { get; }
    public ITable StatementsTable { get; }
    public Instance_ ApiHost { get; }
    public Role ApiRole { get; }

    public StatementVaultStack(Construct scope, string id, StatementVaultStackProps props)
        : base(scope, id, props)
    {
        var namePrefix = $"{props.Project}-{props.EnvName}";

        StatementsBucket = new Bucket(this, "StatementsBucket", new BucketProps
        {
            BucketName = Fn.Join("-", [props.Project, props.EnvName, Aws.ACCOUNT_ID]),
            Encryption = BucketEncryption.S3_MANAGED,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            ObjectOwnership = ObjectOwnership.BUCKET_OWNER_ENFORCED,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN
        });

        StatementsTable = new Table(this, "StatementsTable", new TableProps
        {
            TableName = $"{namePrefix}-statements",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "PK",
                Type = AttributeType.STRING
            },
            SortKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "SK",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.RETAIN
        });

        ApiRole = new Role(this, "ApiRole", new RoleProps
        {
            RoleName = $"{namePrefix}-api",
            AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
            Description = "StatementVault API instance role"
        });

        ApiRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "StatementObjects",
            Actions = ["s3:GetObject", "s3:PutObject"],
            Resources = [StatementsBucket.ArnForObjects("*")]
        }));
        ApiRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "ListStatementBucket",
            Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
            Resources = [StatementsBucket.BucketArn]
        }));
        ApiRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "StatementMetadata",
            Actions = ["dynamodb:GetItem", "dynamodb:PutItem", "dynamodb:Query"],
            Resources = [StatementsTable.TableArn]
        }));

        // Dedicated public VPC so `cdk synth` does not require an account VPC lookup.
        // Terraform's comparison stack uses the account default VPC instead.
        var vpc = new Vpc(this, "Vpc", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 0,
            RestrictDefaultSecurityGroup = false,
            SubnetConfiguration =
            [
                new SubnetConfiguration
                {
                    Name = "public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24
                }
            ]
        });

        var securityGroup = new SecurityGroup(this, "ApiSecurityGroup", new SecurityGroupProps
        {
            Vpc = vpc,
            Description = "StatementVault API host",
            AllowAllOutbound = true
        });
        securityGroup.AddIngressRule(Peer.Ipv4(props.AllowedCidr), Port.Tcp(props.ApiPort), "API");

        var userData = UserData.ForLinux();
        userData.AddCommands(HostUserData.Render(
            Region,
            StatementsBucket.BucketName,
            StatementsTable.TableName,
            props.ApiPort,
            props.ApiImage));

        ApiHost = new Instance_(this, "ApiHost", new InstanceProps
        {
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            InstanceType = new InstanceType(props.InstanceTypeName),
            MachineImage = MachineImage.LatestAmazonLinux2023(new AmazonLinux2023ImageSsmParameterProps
            {
                CpuType = AmazonLinuxCpuType.X86_64
            }),
            Role = ApiRole,
            SecurityGroup = securityGroup,
            UserData = userData,
            AssociatePublicIpAddress = true,
            RequireImdsv2 = true,
            BlockDevices =
            [
                new BlockDevice
                {
                    DeviceName = "/dev/xvda",
                    Volume = BlockDeviceVolume.Ebs(20, new EbsDeviceProps
                    {
                        Encrypted = true,
                        VolumeType = EbsDeviceVolumeType.GP3
                    })
                }
            ]
        });
        Amazon.CDK.Tags.Of(ApiHost).Add("Name", $"{namePrefix}-api");

        new CfnOutput(this, "BucketName", new CfnOutputProps
        {
            Value = StatementsBucket.BucketName,
            Description = "S3 bucket that stores statement files."
        });
        new CfnOutput(this, "TableName", new CfnOutputProps
        {
            Value = StatementsTable.TableName,
            Description = "DynamoDB table that stores statement metadata."
        });
        new CfnOutput(this, "InstancePublicDns", new CfnOutputProps
        {
            Value = ApiHost.InstancePublicDnsName,
            Description = "Public DNS of the API EC2 instance."
        });
        new CfnOutput(this, "InstancePublicIp", new CfnOutputProps
        {
            Value = ApiHost.InstancePublicIp,
            Description = "Public IP of the API EC2 instance."
        });
        new CfnOutput(this, "InstanceRoleArn", new CfnOutputProps
        {
            Value = ApiRole.RoleArn,
            Description = "IAM role assumed by the API instance profile."
        });
        new CfnOutput(this, "ApiBaseUrl", new CfnOutputProps
        {
            Value = $"http://{ApiHost.InstancePublicDnsName}:{props.ApiPort}",
            Description = "HTTP base URL if the security group allows your client."
        });
    }
}
