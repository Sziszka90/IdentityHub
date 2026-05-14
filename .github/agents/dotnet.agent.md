title: Default .NET & C# Agent Instructions
description: Coding agent instructions for .NET and C# projects

# agent: true

# .NET & C# Coding Agent Instructions

## General Guidelines

- Ensure all code is written to support code completion in modern IDEs (e.g., Visual Studio, VS Code, Rider).

- Follow .NET and C# best practices for code structure, naming, and formatting.
- Use PascalCase for class, method, and property names.
- Use camelCase for local variables and parameters.
- Always use explicit access modifiers (public, private, etc.).
- Prefer async/await for asynchronous operations.
- Use dependency injection for services and repositories.
- Write XML documentation comments for public APIs.
- Organize code into appropriate namespaces and folders.
- Use nullability annotations and handle nulls safely.
- Write unit tests for all business logic.

## File Structure

- Place domain models in the Domain layer.
- Place DTOs in the API/DTOs folder, organized by domain.
- Place repository and service implementations in the Infrastructure layer.
- Place interfaces in the Application layer.

## Entity Framework Core

- Use DbContext for data access.
- Use migrations for schema changes.
- Prefer async LINQ methods (e.g., ToListAsync, FirstOrDefaultAsync).
- Avoid business logic in DbContext or entities.

## API Design

- Use RESTful conventions for controllers and endpoints.
- Validate all incoming requests with data annotations or FluentValidation.
- Return IActionResult or ActionResult<T> from controllers.
- Use DTOs for all API input/output.

## Security

- Use ASP.NET Core Identity or Entra ID for authentication.
- Use RBAC (roles, permissions, groups, users) for authorization.
- Never store secrets in code; use configuration providers.

## Documentation

- Document all public APIs and endpoints.
- Keep README and API docs up to date.

## Code Review

- Ensure code is clean, readable, and maintainable.
- Remove dead code and unused references.
- Follow SOLID principles and design patterns where appropriate.

---
