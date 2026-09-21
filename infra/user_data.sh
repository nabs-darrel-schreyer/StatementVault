#!/bin/bash
set -euo pipefail

dnf update -y
dnf install -y docker
systemctl enable --now docker
usermod -aG docker ec2-user

cat >/etc/statementvault.env <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Aws__Region=${region}
Aws__BucketName=${bucket_name}
Aws__TableName=${table_name}
Aws__ServiceUrl=
Aws__ForcePathStyle=false
Aws__CreateResources=false
AWS_DEFAULT_REGION=${region}
EOF

cat >/home/ec2-user/STATEMENTVAULT.md <<'EOF'
# StatementVault host

Docker is installed. The API uses the instance profile for S3 and DynamoDB.
Do not put access keys on this host.

## If terraform.api_image was set
The instance attempted `docker pull` / `docker run` for that image.

## If api_image was empty
Publish the API, then run it:

```bash
# from a workstation
dotnet publish src/StatementVault.Api/StatementVault.Api.csproj -c Release
docker build -f src/StatementVault.Api/Dockerfile -t statementvault-api .
docker save statementvault-api | ssh ec2-user@<instance> docker load

# on the instance
docker run -d --name statementvault-api --restart unless-stopped \
  --env-file /etc/statementvault.env \
  -p ${api_port}:8080 \
  statementvault-api
```

Or push the image to ECR and re-apply Terraform with `-var='api_image=...ecr.../statementvault-api:tag'`.
EOF

chown ec2-user:ec2-user /home/ec2-user/STATEMENTVAULT.md

IMAGE="${api_image}"
if [ -n "$IMAGE" ]; then
  docker pull "$IMAGE"
  docker rm -f statementvault-api >/dev/null 2>&1 || true
  docker run -d --name statementvault-api --restart unless-stopped \
    --env-file /etc/statementvault.env \
    -p ${api_port}:8080 \
    "$IMAGE"
fi
