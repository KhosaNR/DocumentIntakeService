# Document Intake & Processing Service

This is a backend service for ingesting, storing, and processing documents. It is built with .NET 8 and designed to be compatible with AWS (S3/SQS) but runs locally using in-memory stubs.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) OR [Docker](https://www.docker.com/)

## Running Locally via Docker
The easiest way to run the service is using Docker.

1. Open your terminal in the root directory of the repository.
2. Build and start the container:
   ```bash
   docker build -t document-intake .
   docker run -p 8080:8080 document-intake