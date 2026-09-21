output "bucket_name" {
  description = "S3 bucket that stores statement files."
  value       = aws_s3_bucket.statements.bucket
}

output "table_name" {
  description = "DynamoDB table that stores statement metadata."
  value       = aws_dynamodb_table.statements.name
}

output "instance_public_dns" {
  description = "Public DNS of the API EC2 instance."
  value       = aws_instance.api.public_dns
}

output "instance_public_ip" {
  description = "Public IP of the API EC2 instance."
  value       = aws_instance.api.public_ip
}

output "instance_role_arn" {
  description = "IAM role assumed by the API instance profile."
  value       = aws_iam_role.api.arn
}

output "api_base_url" {
  description = "HTTP base URL if the security group allows your client."
  value       = "http://${aws_instance.api.public_dns}:${var.api_port}"
}
