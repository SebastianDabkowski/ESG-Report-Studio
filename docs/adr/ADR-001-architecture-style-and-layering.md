# ADR-001: Architecture style and layering rules (React frontend + .NET API)

Status: Proposed  
Date: 2026-01-27  

## Context

The system is built as a frontend–backend solution with a React frontend and a .NET (C#) HTTP API backend.  
Clear architectural rules are required to maintain consistency, control coupling, and support long-term evolution.

This ADR defines the architecture style, layering rules, dependency directions, and integration boundaries.

## Decision

The system adopts a layered architecture with a clear separation between frontend and backend.

Frontend:
React application organized by feature modules.

Backend:
.NET Web API using an N-Layer architecture:
Api (Presentation) → Application → Domain, with Infrastructure providing technical implementations.

Strict dependency and boundary rules apply. Domain and Infrastructure concerns must never leak across layers.

## Backend layering rules

### Api (Presentation)

Responsibilities:
- HTTP endpoints, routing, controllers
- Authentication and authorization
- Request validation and error mapping
- API versioning and OpenAPI exposure

Rules:
- No direct database access
- No EF Core usage
- Communicates only with Application layer
- Uses DTOs exclusively

### Application

Responsibilities:
- Use cases, commands, queries
- Orchestration and workflows
- Authorization at use-case level
- Transaction boundaries
- Mapping between DTOs and Domain objects

Rules:
- Defines interfaces for persistence and integrations
- Does not reference Infrastructure
- Does not expose Domain entities outside

### Domain

Responsibilities:
- Business rules and invariants
- Aggregates, entities, value objects
- Domain services and domain events

Rules:
- Pure domain logic
- No dependencies on frameworks or infrastructure
- No knowledge of persistence or transport

### Infrastructure

Responsibilities:
- EF Core DbContext and migrations
- Repository implementations
- External system adapters
- Technical services (email, files, background jobs)

Rules:
- Implements interfaces defined in Application
- May reference Application and Domain
- Must not contain business logic

## Dependency rules

Allowed:
- Api → Application
- Application → Domain
- Infrastructure → Application / Domain

Disallowed:
- Domain → any other layer
- Application → Infrastructure
- Api → Infrastructure (except DI composition root)

## Backend boundary rules

- HTTP contracts use DTOs only
- Domain and EF entities must never cross the API boundary
- Persistence models are internal to Infrastructure
- Errors are returned using standardized ProblemDetails
- API versioning should be introduced early if the API is public

## Frontend architecture rules

- React application structured by features
- Each feature owns its UI, state, and API calls
- Shared API client for HTTP, auth headers, and error handling
- No database-shaped models in frontend
- No direct coupling between features

Recommended structure:
- src/app
- src/features
- src/shared

## Frontend–backend integration

- Backend API is the single integration contract
- Communication via HTTP only
- No shared runtime code between frontend and backend
- DTO changes require coordinated updates
- OpenAPI-based client generation is recommended

## Alternatives considered

- UI-coupled backend rendering
- Microservices
- Full vertical slice architecture
- Backend-for-Frontend

All were rejected or deferred due to complexity or premature overhead.

## Consequences

Positive:
- Clear ownership and boundaries
- Easier testing and refactoring
- Reduced architectural drift

Negative:
- Additional boilerplate
- Requires discipline and enforcement

