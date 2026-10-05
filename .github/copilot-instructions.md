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
- Do not use `.Bind(...)`, `.Validate(...)`, or `.ValidateOnStart()` when registering configuration options unless explicitly requested. Read configuration values directly using the existing configuration conventions.

## Architecture Rules

- Use minimal API controllers:
  - Translate HTTP requests to Commands
  - Call Operations via `IOperationMediator`
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
  - Define its own Command record; do not reuse or merge Command records across operations
  - When exposed through an API, have its own Request record; do not reuse or merge Request records across operations, even when their fields overlap
  - Keep each Request limited to fields that operation accepts; update requests must not include immutable fields
  - Validate input via custom Validator in a folder named `Validators`
  - Use injected repositories and services via interfaces
  - Return operation results via `OperationResult<T>`, where `T` is a ReadModel, Result, primitive type, or another application-layer output type
  - Never return persistence Entity classes directly

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
- Never return an `Entity` class, including as the generic type inside `OperationResult<T>`.
- Define one corresponding `ReadModel` for each `Entity` that an operation exposes.
- Map repository `Entity` instances to the appropriate `ReadModel` before returning them from an operation.
- Keep all application-layer mapper extension classes under `Application/Extensions/Mappers`. Use source-entity-specific names such as `SubscriptionEntityMapperExtension` and implement mappings as static extension methods, following the `UserModelMapper` pattern.
- Operations must call mapper extensions instead of defining or inlining entity-to-model mappings.
- In operation files, name repository-returned persistence entities `entity` rather than using a domain noun such as `user`; the operation context already identifies the domain.
- Name values returned by application mapper extensions `model` rather than `response`, since they are application models, not HTTP responses.
- When a required entity is missing, return the appropriate operation failure instead of synthesizing a ReadModel with hard-coded, default, or null placeholder values.
- Keep API `Response` models separate from operation `ReadModel` types. Endpoints must explicitly map only the fields intended for the HTTP contract; do not return a ReadModel directly when its fields should be omitted.
- Use a focused `Result` class or record when an operation returns a composed output that is not a single entity read model.
- Return primitive types or other immutable application-layer types when they are sufficient for the use case.
- Keep exactly one operation class per file.
- Define the operation's Command record in the same file, after the operation class.
- Keep operation orchestration shallow: repository and service calls should be made at a maximum depth of one.
- Do not implement major business logic in nested depth-two-or-greater operation flows; extract it into a focused operation, helper, validator, or domain component.

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
- Apply the explicit mapping rules below; for statuses without a prescribed mapping, choose the appropriate HTTP status per use case.
- Use HTTP 404 (`Results.NotFound(...)`) only when the requested route does not exist; do not use it when an operation cannot find a requested resource.
- Map `OperationStatus.NotFound` (a missing resource) to HTTP 422 with `Results.UnprocessableEntity(result.Error)`, not `Results.NotFound(result.Error)`.
- For successful PUT, PATCH, and DELETE updates, prefer HTTP 204 with `Results.NoContent()` instead of returning a response body. Return a response representation only when it is required by the endpoint contract or its consumers, and document 204 with `.Produces(StatusCodes.Status204NoContent)`.
- In endpoint handlers, map request fields to operation Commands explicitly at the call site; do not use mappers for Request-to-Command conversion.
- Map completed `OperationResult` values to API Response models by explicitly constructing the response in the endpoint; do not use mapper extensions or helper mappers for OperationResult-to-Response conversion.
- Use named arguments for every mapped field when constructing destination models, including application mapper extensions and endpoint Request-to-Command and OperationResult-to-Response mappings.
- Keep request and response models specific to one endpoint at the bottom of that endpoint's file, outside the endpoint class and in the same file-scoped namespace.
- Keep models shared across endpoints in `Api/Models`.
- Never return OperationResult directly from controllers.

## Model & DTO Naming (Strict Semantics)

Use suffixes correctly:

- `Entity` → persistence models (MongoDB)
- `ReadModel` → application/domain models
- `Request` → API input
- `Response` → API output
- `InputMessage` → Bus messaging input
- `OutputMessage` → Bus messaging output
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
- Repositories may return only persistence entities, primitive values, or collections of entities; do not return paginated wrappers or composed result models.
- Operations must call repository count methods when needed and assemble pagination metadata and result models themselves.
- Filters must be passed explicitly via Filter DTOs.
- Prefer fluent MongoDB query async methods such as `query.LongCountAsync()` and `query.ToListAsync()` over static `MongoQueryable` calls such as `MongoQueryable.LongCountAsync(query)`.
- MongoDB entities must remain persistence-focused.
- MongoDB indexes are created in the codebase and ensured by the repository layer to the DB.
- EnsureIndexesAsync method is required in repositories with one empty line distance with actual methods.

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
- When mapping a `List<T>` to another list, prefer `List.ConvertAll(...)` over
  `List.Select(...).ToList()`.
- Do not assign business defaults to enum properties in their declarations. For example, do not write `public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;`; assign enum values explicitly in the operation or factory that creates the entity.
- Do not place function or repository calls directly in `if` conditions. Assign the result to a clearly named local variable first, then evaluate that variable:
  ```csharp
  var user = await repository.Users.GetByIdAsync(command.UserId);
  if (user is null)
  {
      // ...
  }
  ```

## Forbidden

- No exception-driven control flow
- No business logic in controllers
- No cross-layer dependencies
- No ambiguous naming
