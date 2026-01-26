# GitHub Copilot Instructions – Logistics Returns Data Upload Service

## Repository Structure Reference

Folder structure rules are defined in:

- `.github/copilot/folder-structure.md`

Copilot must follow that document when suggesting:

- new folders
- file locations
- test placement

## General

- Target framework is **.NET 10**.
- Use **ASP.NET Core Web API** conventions.
- Use Operation-based **Clean Architecture** strictly.
- Do NOT introduce new libraries or NuGet packages unless explicitly requested.
- Prefer clarity and explicitness over cleverness.

## Architecture Rules

- Use minimal API controllers:
  - Translate HTTP requests to Commands
  - Call Operations via OperationService
  - Map OperationStatus to HTTP responses
- **No business logic** in controllers.
- All orchestration logic belongs in the **Application** layer.
- **Infrastructure** layer handles persistence and external integrations only.
- Layers communicate **only via interfaces**.

## Use-Case / Operation Design

- Every use case must be implemented as a single **Operation**.
- Do NOT create generic Service classes for business logic.
- Each operation must:
  - Have one responsibility
  - Accept a Command which are defined as immutable records
  - Validate input via custom Validator in a folder named `Validators`
  - Use injected repositories and services via interfaces
  - Return Operation results via `OperationResult<T>`

### Naming Conventions (Strict)

- Use Case: `[Verb][DomainConcept]`
  Example: `AddListingFile`
- Operation class: `[UseCaseName]Operation`
- Command: `[UseCaseName]Command`
- Validator: `[UseCaseName]CommandValidator`

Do not deviate from these names.

## Operation Structure

Each operation file may include:

- Operation class implementing `IOperation`
- Command record implementing `IOperationCommand`
- Command validator

Operations must:

- Never throw exceptions for flow control
- Use OperationResult to report all outcomes

## OperationResult Rules

- Always return `OperationResult<T>` from operations.
- Do NOT throw exceptions for validation or domain errors.
- Use the correct `OperationStatus`:
  - Completed
  - NoOperation
  - Invalid
  - NotFound
  - Unauthorized
  - Unprocessable
  - Failed
- Populate error messages explicitly and consistently.

## Controllers & HTTP Mapping

- Controllers must map `OperationStatus` to HTTP responses explicitly.
- Do NOT assume a fixed mapping; choose the appropriate HTTP status per use case.
- Never return OperationResult directly from controllers.

## Model & DTO Naming (Strict Semantics)

Use suffixes correctly:

- `Entity` → persistence models (MongoDB)
- `ReadModel` → application/domain models
- `Request` → API input
- `Response` → API output
- `Command` → input to operations
- `Result` → output from services
- `Setting` → environment-loaded configuration
- `Config` → configuration DTOs passed to services
- `Filter` → repository filtering
- `Value` → immutable value objects
- `Dto` → only when no other suffix applies

Do not mix these roles.
Apart from Entities, all other types are preferably immutable flat records.

## Persistence & Repositories

- Use Repository Pattern with a Repository Manager.
- Repositories must not contain business logic.
- Filters must be passed explicitly via Filter DTOs.
- MongoDB entities must remain persistence-focused.

## Azure Integrations

- Azure Blob Storage interactions belong in Infrastructure only.
- Assume Managed Identity is used.
- Do not hardcode secrets or connection strings.

## Validation

- All command validation must be explicit.
- Validation failures must return `OperationStatus.Invalid`.
- Do not rely on controller-level validation attributes alone.

## Testing

- Unit tests go under `test/unit`.
- Integration tests use **Testcontainers**.
- Do not mock MongoDB or Blob Storage in integration tests.
- Integration tests may override authentication using the Test scheme.

## Coding Style

- Use async/await consistently.
- Avoid static state.
- Prefer immutable records where possible.
- Methods should be small and focused.
- Be explicit rather than implicit.

## Forbidden

- No exception-driven control flow
- No business logic in controllers
- No cross-layer dependencies
- No ambiguous naming
