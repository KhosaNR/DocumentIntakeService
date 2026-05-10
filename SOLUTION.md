# Core Document Intake & Processing Service - Design & Trade-offs

## Architecture Overview

This solution is built using C# and .NET 8 Minimal APIs. It follows a Clean Architecture folder structure, using dependency injection to separate the web layer, infrastructure (storage and state management), domain logic, and asynchronous background processing.

While the system is designed with the AWS cloud path in mind (S3 for object storage, SQS for message queuing, and DynamoDB/RDS for metadata), it currently runs entirely locally using in-memory implementations to satisfy the assignment constraints.

## Key Components

1. **API Endpoints (Intake & Retrieval)**: Handled via .NET 8 Minimal APIs mapped as extension methods in the `Api` layer. DTOs (Data Transfer Objects) are used to prevent leaking internal domain models to the consumer.

2. **Global Exception Handling**: Implements `IExceptionHandler` to capture unhandled runtime errors, logging them and returning a clean, standardized `ProblemDetails` response.

3. **IDocumentRepository**: Manages metadata, status, and the audit trail. Currently backed by a `ConcurrentDictionary` in the `Infrastructure` layer.

4. **IStorageService**: Handles raw file storage. Currently backed by an in-memory dictionary in the `Infrastructure` layer.

5. **IMessageQueue**: Facilitates decoupled communication between the intake API and the background worker. Implemented using `System.Threading.Channels` for safe, high-performance in-memory queuing.

6. **DocumentProcessingWorker**: An `IHostedService` in the `Application` layer that listens to the queue, simulates processing by generating an excerpt, updates the document status, and records an audit trail entry.

## Design Decisions and Trade-offs

### 1. In-Memory Implementations vs. Local Emulators

**Decision**: I opted for purely in-memory abstractions (`ConcurrentDictionary` and `System.Threading.Channels`) rather than requiring LocalStack (for AWS S3/SQS emulation) or a local database.
**Trade-off**: This guarantees the service runs immediately on any machine without complex Docker Compose dependencies or configuration issues. The trade-off is data loss on restart, which is acceptable for this specific vetting exercise.

### 2. Deduplication Strategy

**Decision**: Deduplication is handled using a composite key: `{Provider}_{SourceDocumentId}`. During intake, the system checks if this key exists. If it does, it skips file storage and queueing, appends a "Duplicate Submission Received" event to the audit trail, and returns the existing internal Document ID.
**Trade-off**: This prevents redundant processing and storage costs. However, it assumes the upstream provider does not recycle `SourceDocumentId`s for different documents.

### 3. Background Processing via `System.Threading.Channels`

**Decision**: The `IMessageQueue` is implemented using .NET Channels rather than a simple `ConcurrentQueue`.
**Trade-off**: Channels provide built-in async support, backpressure handling, and thread safety, making it a much closer behavioral match to a real cloud queue (like AWS SQS) than basic collections. It keeps the background worker in-process, satisfying the "no separate process required" constraint.

### 4. Clean Architecture Folder Structure

**Decision**: The project is organized into `Domain`, `Application`, `Infrastructure`, and `Api` folders.
**Trade-off**: Structuring the code cleanly within a single project reduces the overhead of managing multiple `.csproj` references while clearly demonstrating separation of concerns. It is highly readable for code reviews and scales well if the team decides to extract layers into distinct projects later.