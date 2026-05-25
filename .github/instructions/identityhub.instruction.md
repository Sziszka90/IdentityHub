# IdentityHub: Full Project & Authorization Model Guide

---

# Project Overview

**IdentityHub** is an enterprise-grade authentication and authorization service that sits between Azure Entra ID and your applications, providing tenant-aware identity management, role-based access control (RBAC), and permission-based authorization.

## 🎯 Core Purpose

IdentityHub answers one critical question:

> **"Who is this user and what are they allowed to do?"**

It acts as an **integration and decision layer** on top of Azure Entra ID, providing:

- ✅ Centralized authentication via OIDC
- ✅ Role and permission-based authorization
- ✅ Multi-tenant identity management
- ✅ Graph API integration for user and group data
- ✅ Configurable client-side caching with memory or Redis-backed distributed cache
- ✅ Audit logging and compliance

---

# Architecture

```
┌─────────────┐
│ Application │
└──────-┬─────┘
        │
        ▼
┌──────────────────────────────┐
│ IdentityHub.Client NuGet pkg │ ← Typed client + authorization integration
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ IdentityHub.Contracts NuGet  │ ← Shared public DTOs used by client and API
└────────────┬─────────────────┘
          │ "Can user X do Y?"
          ▼
┌──────────────────────────────┐
│        IdentityHub.API       │ ← Authentication, Authorization Engine
└────────────┬─────────────────┘
            │
            ▼
┌──────────────────────────────┐
│      Azure Entra ID          │ ← Identity Provider
│        + Graph API           │
└──────────────────────────────┘
```

**What IdentityHub IS:**

- An authorization decision service
- A Graph API integration layer
- A tenant-aware permission resolver
- A set of NuGet packages for easy .NET integration:
    - `IdentityHub.Client` for typed API calls and permission-based authorization
    - `IdentityHub.Contracts` for shared DTOs and response/request contracts

**What IdentityHub is NOT:**

- An identity provider (Entra ID handles that)
- A custom login system
- A password manager
- A replacement for Azure AD

**How the NuGet Packages Work:**

- The `IdentityHub.Client` NuGet package is added to your application to provide the strongly-typed HTTP client and authorization integration.
- The `IdentityHub.Contracts` NuGet package contains the shared DTOs used by both the client package and the API surface.
- Register the client in your app with `services.AddIdentityHubClient(configuration);`.
- Optionally enable permission attributes in the consuming application with `services.AddIdentityHubAuthorization();`.
- The client supports configurable caching for authorization config and permission checks:
    - `Memory` cache for single-instance or local development scenarios
    - `Distributed` cache backed by Redis for multi-instance deployments
- The application uses the client to answer authorization questions ("Can user X do Y?") by querying IdentityHub.API, which in turn integrates with Entra ID and Graph API.

**Example Project for Client Testing:**

- The solution includes `IdentityHub.ExampleProject` as a small consumer app for validating the `IdentityHub.Client` NuGet package.
- It includes a mocked IdentityHub endpoint at `mock-identityhub/api/authorization/check` so the client can be exercised without a live IdentityHub.API dependency.
- The example controller exposes a fixed endpoint and cache diagnostics so you can test `RequirePermissionAttribute`, client configuration, and cache behavior end to end.
- The mocked authorization check accepts a simple bearer token pattern during local testing and returns deterministic allow/deny responses for the client integration flow.

---

# Technology Stack

| Layer              | Technology                                |
| ------------------ | ----------------------------------------- |
| **Backend**        | .NET                                      |
| **Authentication** | Azure Entra ID (OIDC, JWT)                |
| **Identity Data**  | Microsoft Graph API                       |
| **Authorization**  | RBAC, Group & Permission                  |
| **Data Storage**   | SQL Server                                |
| **Client Caching** | IMemoryCache / Redis                      |
| **Packages**       | IdentityHub.Client, IdentityHub.Contracts |
| **Logging**        | Azure Application Insights                |
| **Identity**       | Azure Managed Identity                    |
| **Frontend**       | Angular                                   |

---

# Authorization Model Cheat Sheet

## Key Concepts

- **User**: An individual identity (from Entra ID/Azure AD). Users are not directly assigned roles or permissions.
- **Group**: An Entra ID group. Users are members of groups.
- **GroupRoleMapping**: Maps a group to a single application role. If a user is in a group, they get the mapped role.
- **Role**: A named set of permissions (e.g., Admin, User, Viewer). Roles are assigned to users via group membership.
- **RolePermission**: Many-to-many join between roles and permissions. Each role can have many permissions; each permission can belong to many roles.
- **Permission**: A granular action (e.g., `users.read`, `orders.delete`). Roles aggregate permissions.

## How It Works

User → Groups → Roles → Permissions

- When a user logs in, their group memberships are mapped to roles.
- Each role gives the user a set of permissions.
- The user’s effective permissions are the union of all permissions from all their roles.

## Example: Full Authorization Tree

Suppose you have the following setup:

- **Users:**
    - Alice
    - Bob
    - Carol
- **Groups:**
    - HR-Team
    - Global-Admins
    - Support-Agents
- **GroupRoleMapping:**
    - HR-Team → HRManager
    - Global-Admins → Admin
    - Support-Agents → SupportAgent
- **Roles:**
    - **Admin**: `users.*`, `groups.*`, `roles.*`, `audit.*`
    - **HRManager**: `users.read`, `users.invite`, `profile.update`
    - **SupportAgent**: `tickets.read`, `tickets.update`, `users.read`
- **Permissions:**
    - `users.read` (view users)
    - `users.invite` (invite users)
    - `profile.update` (update profile)
    - `users.*` (all user actions)
    - `groups.*` (all group actions)
    - `roles.*` (all role actions)
    - `audit.*` (all audit actions)
    - `tickets.read` (view tickets)
    - `tickets.update` (update tickets)

### User Memberships

- **Alice**: HR-Team, Global-Admins
- **Bob**: Support-Agents
- **Carol**: HR-Team

### Effective Roles and Permissions

| User  | Groups                 | Roles            | Permissions                                                                   |
| ----- | ---------------------- | ---------------- | ----------------------------------------------------------------------------- |
| Alice | HR-Team, Global-Admins | HRManager, Admin | users.read, users.invite, profile.update, users._, groups._, roles._, audit._ |
| Bob   | Support-Agents         | SupportAgent     | tickets.read, tickets.update, users.read                                      |
| Carol | HR-Team                | HRManager        | users.read, users.invite, profile.update                                      |

### Tree Diagram

```mermaid
graph TD
    Alice --> HR-Team
    Alice --> Global-Admins
    Bob --> Support-Agents
    Carol --> HR-Team

    HR-Team --> HRManager
    Global-Admins --> Admin
    Support-Agents --> SupportAgent

    HRManager --> users.read
    HRManager --> users.invite
    HRManager --> profile.update

    Admin --> users.*
    Admin --> groups.*
    Admin --> roles.*
    Admin --> audit.*

    SupportAgent --> tickets.read
    SupportAgent --> tickets.update
    SupportAgent --> users.read
```

This diagram shows how users, groups, roles, and permissions are connected in the authorization model.

---

## Example: Permission Entity

Here’s how a `Permission` object is represented in code and how it fits into the model:

```csharp
public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "users.read"
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
```

### Example Permissions in the System

| Id  | Name           | Description       | CreatedAt           |
| --- | -------------- | ----------------- | ------------------- |
| 1   | users.read     | View users        | 2024-01-01 00:00:00 |
| 2   | users.invite   | Invite users      | 2024-01-01 00:00:00 |
| 3   | profile.update | Update profile    | 2024-01-01 00:00:00 |
| 4   | users.\*       | All user actions  | 2024-01-01 00:00:00 |
| 5   | groups.\*      | All group actions | 2024-01-01 00:00:00 |
| 6   | roles.\*       | All role actions  | 2024-01-01 00:00:00 |
| 7   | audit.\*       | All audit actions | 2024-01-01 00:00:00 |
| 8   | tickets.read   | View tickets      | 2024-01-01 00:00:00 |
| 9   | tickets.update | Update tickets    | 2024-01-01 00:00:00 |

Each `Permission` is linked to one or more roles via `RolePermission`. For example, the `Admin` role will have `RolePermission` entries for `users.*`, `groups.*`, `roles.*`, and `audit.*`.

### Example: RolePermission Join

| Role         | Permission     |
| ------------ | -------------- |
| Admin        | users.\*       |
| Admin        | groups.\*      |
| Admin        | roles.\*       |
| Admin        | audit.\*       |
| HRManager    | users.read     |
| HRManager    | users.invite   |
| HRManager    | profile.update |
| SupportAgent | tickets.read   |
| SupportAgent | tickets.update |
| SupportAgent | users.read     |

---
