namespace StatementVault.Infra;

internal static class HostUserData
{
    public static string Render(string region, string bucketName, string tableName, int apiPort, string? apiImage)
    {
        var image = apiImage ?? string.Empty;
        return $$"""
            #!/bin/bash
            set -euo pipefail

            dnf update -y
            dnf install -y docker
            systemctl enable --now docker
            usermod -aG docker ec2-user

            cat >/etc/statementvault.env <<EOF
            ASPNETCORE_ENVIRONMENT=Production
            ASPNETCORE_URLS=http://+:8080
            Aws__Region={{region}}
            Aws__BucketName={{bucketName}}
            Aws__TableName={{tableName}}
            Aws__ServiceUrl=
            Aws__ForcePathStyle=false
            Aws__CreateResources=false
            AWS_DEFAULT_REGION={{region}}
            EOF

            cat >/home/ec2-user/STATEMENTVAULT.md <<'EOF'
            # StatementVault host

            Docker is installed. The API uses the instance profile for S3 and DynamoDB.
            Do not put access keys on this host.

            Publish the API, then run it, or re-deploy CDK with -c apiImage=... after pushing to ECR.
            EOF

            chown ec2-user:ec2-user /home/ec2-user/STATEMENTVAULT.md

            IMAGE="{{image}}"
            if [ -n "$IMAGE" ]; then
              docker pull "$IMAGE"
              docker rm -f statementvault-api >/dev/null 2>&1 || true
              docker run -d --name statementvault-api --restart unless-stopped \
                --env-file /etc/statementvault.env \
                -p {{apiPort}}:8080 \
                "$IMAGE"
            fi
            """;
    }
}
