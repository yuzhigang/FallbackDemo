# Sisyphus Agent Guide - FallbackDriver

This document serves as the authoritative guide for agentic development within the `FallbackDriver` repository. All agents must adhere to these standards to ensure consistency, maintainability, and architectural integrity.

## 0. Introduction
Welcome to the `FallbackDriver` development environment. As an agent, your goal is to write high-quality, production-ready C# code that fits seamlessly into a .NET 8 ecosystem. This guide captures the implicit requirements, architectural decisions, and coding standards that define this project. Use it as your primary reference when implementing new features or fixing bugs.

## 1. Project Overview
`FallbackDriver` is a high-performance data compensation framework built on **.NET 8**. Its primary purpose is to provide a "second line of defense" for data acquisition. When primary drivers fail to retrieve telemetry data (Tags) due to network issues, timeouts, or driver-specific errors, this system intercepts the failure, generates compensation tasks, and executes them via specialized fallback drivers.

The system is designed to be:
- **Resilient**: Minimizes data loss in volatile network conditions.
- **Extensible**: Allows easy integration of new fallback sources (e.g., InSQL, Historian, CSV).
- **Efficient**: Uses task merging and async I/O to minimize resource consumption.

## 2. Technical Stack
- **Framework**: .NET 8 (LTS)
- **Language**: C# 12.0
- **Concurrency**: `System.Threading.Channels` for memory-based producer-consumer patterns.
- **Design Pattern**: Interface-based decoupling, Dependency Injection, Strategy pattern for fallback execution.

## 3. Development Commands

### 3.1 Build & Maintenance
- **Restore Dependencies**: `dotnet restore`
- **Build Project**: `dotnet build`
- **Clean Solution**: `dotnet clean`

### 3.2 Testing (Standard Patterns)
- **Run All Tests**: `dotnet test`
- **Run Single Test Class**: `dotnet test --filter "ClassName"`
- **Run Specific Method**: `dotnet test --filter "FullyQualifiedName~MethodName"`
- **Detailed Logs**: `dotnet test --logger "console;verbosity=detailed"`

## 4. Code Style & Conventions

### 4.1 Naming Conventions
| Element | Convention | Example |
| :--- | :--- | :--- |
| Interfaces | `IPascalCase` | `IFallBackDriver` |
| Classes / Structs | `PascalCase` | `InsqlFallbackDriver` |
| Methods | `PascalCase` | `ReadAsync` |
| Properties | `PascalCase` | `StartTime` |
| Private Fields | `_camelCase` | `_channelWriter` |
| Parameters | `camelCase` | `context` |
| Local Variables | `camelCase` | `retryCount` |
| Constant / Enum | `PascalCase` | `MaxRetryLimit` |

### 4.2 Async Patterns
- All methods performing I/O or long-running tasks **must** return `Task` or `ValueTask`.
- Suffix all asynchronous methods with `Async`.
- **CRITICAL**: The existing `IFallBackDriver.ReadAsync` returns `IList<HistoryTagValue>`. This is an architectural debt. New implementations should favor `Task<IList<HistoryTagValue>>`.

### 4.3 Formatting
- **Indentation**: 4 spaces (no tabs).
- **Braces**: Allman style (braces on new lines).
- **Namespaces**: Use file-scoped namespaces (e.g., `namespace FallbackDriver.Core;`).
- **Usings**: Organize alphabetically; `System` namespaces first.

### 4.4 Documentation
- Use XML documentation (`/// <summary>`) for all public interfaces and classes.
- Explain the "Why" in comments, especially for complex merge logic in fallback tasks.

## 5. Architectural Principles

### 5.1 Producer-Consumer via Channels
The system relies on an in-memory queue implemented via `System.Threading.Channels`.
- **Producers**: Primary drivers that detect a "miss" in data. They should push `FallbackTask` objects into the channel without blocking the main telemetry loop.
- **Consumers**: The `IFallBackService` which monitors the channel and dispatches tasks to `IFallBackDriver`. The consumer should be implemented as a `BackgroundService`.

### 5.2 Task Merging Strategy
Agents should implement logic to merge `FallbackTask` objects. If multiple tasks target the same `DriverId` within overlapping or adjacent `DateTime` ranges, they should be consolidated into a single batch request to the `IFallBackDriver` to minimize overhead. This is critical for systems with high-frequency telemetry.

### 5.3 Error Handling & Resilience
- **Fail-Fast**: Validate `FallBackReadContext` parameters (Tags count, Time range) before execution. If the range is too large, split it into smaller chunks.
- **No-Op Safety**: If a `FallbackDriver` is not configured for a specific `DriverId`, the system should log a warning and complete the task without throwing an unhandled exception.
- **Retry Policy**: Implement exponential backoff for transient failures in the fallback drivers.

### 5.4 Logging & Observability
- Use `ILogger` for all logging.
- Include `DriverId` and `CorrelationId` in all log messages related to fallback tasks.
- Log significant events: Task creation, Task merging, Task completion (success/fail), and total records compensated.

## 6. Implementation Checklist for Agents
- [ ] Ensure all new classes have corresponding interfaces if they are intended for DI.
- [ ] Verify that `CancellationToken` is passed through all async chains.
- [ ] Use `record` types for DTOs/Contexts where immutability is preferred.
- [ ] Check for `null` using the `is` operator (e.g., `if (context is null)`).
- [ ] Ensure `IDisposable` is correctly implemented for any resource-heavy drivers.

## 7. Context-Specific Rules
- **Fallback Tasks**: Can represent a single Tag or an entire Driver instance.
- **Identification**: One of the biggest challenges is identifying *exactly* which tags failed. When in doubt, favor over-compensation (reading a slightly larger set) than under-compensation.
- **Configuration**: Mapping between `Driver` and `FallbackDriver` must be loose-coupled (via config or database), never hardcoded.

## 8. Advanced C# 12 Patterns
To maintain a modern codebase, agents should leverage the following C# 12 features where appropriate:

### 8.1 Primary Constructors
Use primary constructors for dependencies in services and drivers to reduce boilerplate:
```csharp
public class FallBackService(ILogger<FallBackService> logger, Channel<FallbackTask> channel) 
    : IFallBackService
{
    private readonly ILogger _logger = logger;
}
```

### 8.2 Collection Expressions
Use `[]` instead of `new List<T>()` or `new T[]` for cleaner initialization:
```csharp
var tags = ["Tag1", "Tag2", "Tag3"];
IList<HistoryTagValue> results = [];
```

## 9. Dependency Management & DI
- All services must be registered in the `IServiceCollection`.
- Prefer `Scoped` or `Singleton` for drivers depending on whether they maintain state.
- Avoid the Service Locator pattern (`IServiceProvider.GetService`); always use Constructor Injection.

## 10. Concurrency Deep Dive: Channels
When working with `System.Threading.Channels`:
- Use `BoundedChannelOptions` with a reasonable `FullMode` (e.g., `Wait` or `DropWrite`) to prevent memory exhaustion.
- The consumer loop should use `await foreach (var task in _channel.Reader.ReadAllAsync(ct))`.
- Always ensure the `CancellationToken` is honored within the consumer loop to allow graceful shutdown.

## 11. Testing & Mocking Strategy
The project uses a standard testing stack. When writing tests:
- **Mocking**: Use `Moq` or `NSubstitute` to mock `IFallBackDriver` and `IDriver`.
- **Data Integrity**: Verify that `HistoryTagValue.Value` is correctly boxed/unboxed according to the `Tag.DataType`.
- **Concurrency Tests**: Use `Task.Delay` and `CancellationTokenSource` to simulate network latency and timeouts in fallback scenarios.

## 12. Git Workflow & Commit Standards
- **Branching**: `feature/`, `fix/`, `refactor/`, `docs/`.
- **Commits**: Follow [Conventional Commits](https://www.conventionalcommits.org/).
  - Example: `feat(core): add channel-based task merging logic`
- **PRs**: Each PR must include a summary of changes and a link to the relevant issue.

## 13. Common Anti-Patterns to Avoid
- **Sync-over-Async**: Never use `.Result` or `.Wait()` on Task objects.
- **Direct Instantiation**: Avoid `new InsqlFallbackDriver()` inside services; use the factory pattern or DI.
- **Hardcoded Strings**: Use `const` or `static readonly` fields for repeated string keys, especially Driver IDs.
- **Ignoring Cancellations**: Never ignore the `CancellationToken` provided by the caller.

---
*This file is managed by Sisyphus. Do not modify the core sections without consulting the project lead.*
