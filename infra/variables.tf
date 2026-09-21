variable "region" {
  type        = string
  description = "AWS region for all resources."
  default     = "us-east-1"
}

variable "env" {
  type        = string
  description = "Short environment name used in resource names (dev, test, prod)."
  default     = "dev"
}

variable "project" {
  type        = string
  description = "Name prefix for resources."
  default     = "statementvault"
}

variable "instance_type" {
  type        = string
  description = "EC2 instance type for the API host."
  default     = "t3.micro"
}

variable "allowed_cidr" {
  type        = string
  description = "CIDR allowed to reach the API on the instance. Do not leave 0.0.0.0/0 in a real bank environment."
  default     = "0.0.0.0/0"
}

variable "api_port" {
  type        = number
  description = "Host port that Docker publishes for the API."
  default     = 8080
}

variable "api_image" {
  type        = string
  description = "Container image to run on the instance, for example 123456789012.dkr.ecr.us-east-1.amazonaws.com/statementvault-api:latest. Leave empty to install Docker and write publish instructions instead of starting a container."
  default     = ""
}
