# Folder Structure – Source of Truth

This document defines the **only valid folder structure** for this repository.
All new files and folders must conform to these rules.

---

## Top-Level Structure

Only the following top-level folders are allowed:

- src/
- test/

Rules:

- All production code must live under `src/`
- All test code must live under `test/`
- Do NOT create new top-level folders

---

## Source Layers (`src/`)

src/
├── Api/
├── Application/
├── Infrastructure/
├── Shared/

Each folder represents a **Clean Architecture layer** with strict responsibilities.

### Api Layer (`src/Api`)

Purpose:

- Entry point of the system
- HTTP concerns only

Allowed:

- Minimal API with individual endpoint files
- API Request / Response models
- Authentication / authorization wiring
- Program.cs and startup configuration
- Mapping HTTP to Commands

Forbidden:

- Business logic
- Persistence logic
- Direct access to Infrastructure implementations
- OperationResult logic creation

### Application Layer (`src/Application`)

Purpose:

- Core business logic and use cases

Allowed:

- Operations (use cases)
- Commands and Validators
- Application Models and Value objects
- Interfaces for repositories and external services
- Orchestration logic

Forbidden:

- HTTP concerns
- Persistence implementations
- Azure, MongoDB, or external SDK usage
- Framework-specific code

### Infrastructure Layer (`src/Infrastructure`)

Purpose:

- Technical implementations of external dependencies

Allowed:

- Repository implementations
- Azure Blob Storage integrations
- MongoDB entities and data access
- External service adapters
- Configuration binding for infrastructure

Forbidden:

- Business rules
- Use-case orchestration
- Controller logic
- Application-level decision making

### Shared Layer (`src/Shared`)

Purpose:

- Cross-cutting concerns shared across layers

Allowed:

- OperationResult and OperationStatus
- Common utilities and helpers
- Shared abstractions
- Constants and cross-layer value objects

Forbidden:

- Business logic specific to a use case
- Infrastructure-specific implementations
- HTTP-specific logic

---

## Test Structure (`test/`)

test/
├── unit/
└── integration/

### Unit Tests (`test/unit`)

Purpose:

- Verify business logic in isolation

Allowed:

- Tests for Application and Shared layers
- Mocked repositories and services

Forbidden:

- Real databases or external services

### Integration Tests (`test/integration`)

Purpose:

- Verify infrastructure and system behaviour

Allowed:

- Testcontainers (MongoDB, Azurite)
- Real repository implementations
- Authentication overrides for testing

Forbidden:

- Mocking infrastructure dependencies
