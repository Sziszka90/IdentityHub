# IdentityHub

**IdentityHub** is an enterprise-grade authentication and authorization service that sits between Azure Entra ID and your applications, providing tenant-aware identity management, role-based access control (RBAC), and permission-based authorization.

## 🎯 Core Purpose

IdentityHub answers one critical question:

> **"Who is this user and what are they allowed to do?"**

It acts as an **integration and decision layer** on top of Azure Entra ID, providing:

- ✅ Centralized authentication via OIDC
- ✅ Role and permission-based authorization
- ✅ Multi-tenant identity management
- ✅ Graph API integration for user and group data
- ✅ Policy-driven access control
- ✅ Audit logging and compliance

---

## 🏗️ Architecture

```
┌─────────────┐
│ Application │
└──────┬──────┘
       │ "Can user X do Y?"
       ▼
┌─────────────────┐
│  IdentityHub    │ ← Authentication, Authorization, Policy Engine
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Azure Entra ID  │ ← Identity Provider
│  + Graph API    │
└─────────────────┘
```

**What IdentityHub IS:**

- An authorization decision service
- A Graph API integration layer
- A tenant-aware permission resolver
- A policy evaluation engine

**What IdentityHub is NOT:**

- An identity provider (Entra ID handles that)
- A custom login system
- A password manager
- A replacement for Azure AD

---

## 🚀 Features

### Phase 1: Core Identity & Authorization (MVP)

#### ✅ Authentication (Entra ID)

- Azure Entra ID login via OIDC
- JWT token validation and claims extraction
- Token-to-user context mapping
- Secure authentication pipeline

#### ✅ User & Identity Data (Graph API)

- Fetch user profiles from Microsoft Graph
- Retrieve group memberships
- Access assigned app roles
- Intelligent caching with short TTL for performance

#### ✅ Authorization Model

- **Role-Based Access Control (RBAC)**: Admin, User, Viewer, etc.
- **Group-to-Role Mapping**: Entra ID groups → application roles
- **Policy-Based Authorization**: `[Authorize(Policy = "...")]`
- Clean separation between authentication and authorization logic

#### ✅ Protected API

- Secured REST endpoints
- Proper HTTP status codes (401 Unauthorized vs 403 Forbidden)
- Clear permission boundaries
- Request validation and error handling

#### ✅ Audit Logging

- User authentication events
- Authorization decisions
- Access denial tracking
- Compliance-ready audit trail

---

### Phase 2: Enterprise-Grade Features

#### 🔹 Multi-Tenant Awareness

- Tenant ID extraction from JWT tokens
- Tenant-scoped authorization rules
- Per-tenant permission isolation
- Same user, different tenants → different permissions

#### 🔹 App Roles + Group-Based Authorization

- Support for both **Entra App Roles** and **Security Groups**
- Configurable mapping strategies
- Hybrid role resolution
- Documented design decisions for each approach

#### 🔹 Permission-Based Access (Fine-Grained)

Beyond simple roles:

- **Granular permissions**: `users.read`, `users.invite`, `billing.view`
- **Roles aggregate permissions**: Admin = [users.*, billing.*]
- **Policy checks evaluate permissions**, not just roles
- Scalable authorization model for enterprise applications

#### 🔹 Admin API

- List users and their effective permissions
- View group → role → permission resolution chain
- Audit user access history
- RESTful management interface (no UI dependency)

---

### Phase 3: Advanced & Differentiating Features

#### 🌟 Policy Engine

- JSON-based declarative policies
- Context-aware authorization:
    - Tenant context
    - Time-based access
    - Role + permission combinations
- Clean, extensible evaluation pipeline

#### 🌟 Managed Identity & Secretless Authentication

- **Zero client secrets** in production
- Managed Identity for Microsoft Graph API access
- Enhanced security posture
- Credential rotation handled by Azure

#### 🌟 Event-Driven Identity Synchronization

- Microsoft Graph change notifications (webhooks)
- Real-time reactions to:
    - User added/removed
    - Group membership changes
    - Role assignment updates
- Production-ready reactive architecture

#### 🌟 Admin UI (Angular)

- Tenant overview dashboard
- Role assignment interface
- Permission visualization
- User management console

---

## 🛠️ Technology Stack

| Layer              | Technology                   |
| ------------------ | ---------------------------- |
| **Backend**        | .NET                         |
| **Authentication** | Azure Entra ID (OIDC, JWT)   |
| **Identity Data**  | Microsoft Graph API          |
| **Authorization**  | Policy-based + RBAC          |
| **Data Storage**   | Azure Cosmos DB / SQL Server |
| **Caching**        | Azure Redis Cache            |
| **Logging**        | Azure Application Insights   |
| **Identity**       | Azure Managed Identity       |
| **Frontend**       | Angular (Phase 3)            |

---

## 📋 Prerequisites

- Azure subscription with Entra ID (Azure AD)
- App registration in Entra ID with appropriate permissions:
    - `User.Read.All`
    - `GroupMember.Read.All`
    - `Directory.Read.All`
- .NET 8 SDK / Node.js 20+ (depending on implementation)
- Azure CLI installed and configured

---

## 🚦 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/IdentityHub.git
cd IdentityHub
```

### 2. Configure Entra ID

```bash
# Create app registration
az ad app create --display-name "IdentityHub" \
  --sign-in-audience AzureADMultipleOrgs \
  --required-resource-accesses @manifest.json
```

### 3. Set Environment Variables

```bash
export ENTRA_TENANT_ID="your-tenant-id"
export ENTRA_CLIENT_ID="your-client-id"
export ENTRA_CLIENT_SECRET="your-client-secret"  # or use Managed Identity
export GRAPH_API_SCOPE="https://graph.microsoft.com/.default"
```

### 4. Run Locally

```bash
# Backend
dotnet run --project src/IdentityHub.API

# Or with Node.js
npm install
npm run dev
```

### 5. Test Authentication

```bash
curl -H "Authorization: Bearer <your-jwt-token>" \
  http://localhost:5000/api/identity/me
```

---

## 🧪 Development Roadmap

### ✅ Phase 1: Foundation (Weeks 1-3)

- [x] Entra ID authentication
- [x] JWT validation pipeline
- [x] User context extraction from claims
- [x] Basic RBAC implementation
- [x] Protected API endpoints
- [x] Clean Architecture refactoring

### 🔄 Phase 2: Enterprise Features (Weeks 4-6)

- [x] Permission model (role-to-permission mapping)
- [x] Group-to-role mapping configuration
- [x] Policy-based authorization (`[Authorize(Policy)]`)
- [x] Permission checking endpoints
- [x] Multi-tenant isolation middleware
- [x] Admin API (user/role/permission management)
- [x] Redis caching layer

### 📅 Phase 3: Advanced (Weeks 7+)

- [x] Graph API integration (user/group enrichment)
- [x] Context-aware policy engine
- [x] Managed Identity support
- [ ] Graph webhooks (change notifications)
- [ ] Admin UI (Angular)
- [ ] Audit logging and compliance

---

## 📦 Using IdentityHub.Authorization as a NuGet Package

`IdentityHub.Application` is published as the `IdentityHub.Authorization` NuGet package. External .NET APIs can embed the full authorization engine — including permission and role handlers, Graph API integration, Redis caching, and policy registration — without running a separate service.

There are two usage models:

---

### Model A — Standalone (config from appsettings, no central service)

The external app carries its own role/permission config in `appsettings.json`. No network calls to IdentityHub.API are made.

**1. Install the package**

```bash
dotnet add package IdentityHub.Authorization
```

**2. Add config to `appsettings.json`**

```json
{
    "Authorization": {
        "GroupToRoleMapping": {
            "YourApp-Admins": "Admin",
            "YourApp-Users": "User"
        },
        "RolePermissions": {
            "Admin": ["orders.*", "users.*"],
            "User": ["orders.read"]
        }
    },
    "AuthorizationPolicies": {
        "PermissionPolicies": {
            "CanReadOrders": "orders.read",
            "CanDeleteOrders": "orders.delete"
        },
        "RolePolicies": {
            "RequireAdmin": "Admin"
        }
    }
}
```

**3. Register in `Program.cs`**

```csharp
builder.Services.AddAuthorizationPolicies(builder.Configuration);
```

**4. Protect endpoints**

```csharp
[Authorize(Policy = "CanDeleteOrders")]
[HttpDelete("{id}")]
public IActionResult Delete(int id) { ... }
```

---

### Model B — Connected (config from central IdentityHub.API service)

The external app reads roles, permissions, and policies from the running IdentityHub.API service over HTTP. Config is managed centrally via the admin API — no need to touch `appsettings.json` per app.

**1. Install the package**

```bash
dotnet add package IdentityHub.Authorization
```

**2. Add config to `appsettings.json`**

```json
{
    "IdentityHubClient": {
        "BaseUrl": "https://your-identityhub-instance.example.com",
        "ApiKey": "your-machine-to-machine-bearer-token",
        "CacheSeconds": 300
    },
    "AuthorizationPolicies": {
        "PermissionPolicies": {
            "CanDeleteOrders": "orders.delete"
        },
        "RolePolicies": {
            "RequireAdmin": "Admin"
        }
    }
}
```

> `CacheSeconds` controls how long the authorization config snapshot is cached in memory before the client re-fetches it from the API.

**3. Register in `Program.cs`**

```csharp
// Registers the HTTP client as IAuthorizationConfigRepository
builder.Services.AddIdentityHubClient(builder.Configuration);

// Registers handlers and policies (reads config from the HTTP client)
builder.Services.AddAuthorizationPolicies(builder.Configuration);
```

**4. Protect endpoints — identical to Model A**

```csharp
[Authorize(Policy = "CanDeleteOrders")]
[HttpDelete("{id}")]
public IActionResult Delete(int id) { ... }
```

**5. Manage config at runtime via IdentityHub.API admin endpoints**

```bash
# Add a new role with permissions
curl -X POST https://your-identityhub-instance.example.com/api/authorization-config/roles \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{ "name": "Auditor", "permissions": ["audit.view"] }'

# Add a permission policy
curl -X POST https://your-identityhub-instance.example.com/api/authorization-config/permission-policies \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{ "policyName": "CanViewAudit", "requiredPermission": "audit.view" }'
```

Changes take effect in external apps within `CacheSeconds` seconds — no restart required.

---

### Comparison

|                            | Model A (standalone)        | Model B (connected)             |
| -------------------------- | --------------------------- | ------------------------------- |
| Config location            | `appsettings.json` per app  | Central IdentityHub DB          |
| Network dependency         | None                        | IdentityHub.API must be running |
| Multiple apps share config | No                          | Yes                             |
| Runtime config changes     | Requires redeploy           | Within `CacheSeconds`           |
| Good for                   | Single app / self-contained | Enterprise / many apps          |

---

## 🏛️ Authorization Model

### Roles

```json
{
    "Admin": ["users.*", "groups.*", "roles.*", "audit.*"],
    "User": ["users.read", "groups.read", "profile.update"],
    "Viewer": ["users.read", "groups.read"]
}
```

### Permissions

Granular actions that roles aggregate:

- `users.read` - View user information
- `users.invite` - Invite new users
- `users.delete` - Remove users
- `groups.manage` - Manage group memberships
- `roles.assign` - Assign roles to users
- `audit.view` - View audit logs

### Policy Example

```json
{
    "policy": "CanManageUsers",
    "conditions": {
        "permissions": ["users.invite", "users.delete"],
        "tenant": "required",
        "mfa": true
    }
}
```

---

## 📊 API Examples

### Get Current User Identity

```http
GET /api/identity/me
Authorization: Bearer <jwt-token>

Response:
{
  "userId": "abc-123",
  "email": "user@example.com",
  "roles": ["User"],
  "permissions": ["users.read", "groups.read"],
  "tenant": "tenant-xyz"
}
```

### Check Permission

```http
POST /api/authorization/check
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "permission": "users.invite",
  "resource": "/api/users"
}

Response:
{
  "allowed": true,
  "reason": "User has Admin role with users.* permissions"
}
```

### Get Effective Permissions

```http
GET /api/admin/users/{userId}/permissions
Authorization: Bearer <admin-jwt-token>

Response:
{
  "userId": "abc-123",
  "roles": ["Admin", "User"],
  "permissions": ["users.*", "groups.*", "roles.*"],
  "groups": ["Global-Admins", "IT-Team"],
  "tenant": "tenant-xyz"
}
```

---

## 🔒 Security Considerations

- ✅ **No passwords stored** - Entra ID handles authentication
- ✅ **Managed Identity** for Graph API (no client secrets in code)
- ✅ **JWT signature validation** on every request
- ✅ **Least privilege principle** for Graph API permissions
- ✅ **Tenant isolation** enforced at authorization layer
- ✅ **Audit logging** for compliance
- ✅ **Rate limiting** on API endpoints

---

## 🧩 Integration Guide

### For Applications Using IdentityHub

```csharp
// In your ASP.NET Core app
services.AddIdentityHub(options =>
{
    options.Authority = "https://identityhub.yourdomain.com";
    options.Audience = "api://your-app";
    options.RequireHttpsMetadata = true;
});

// In your controller
[Authorize(Policy = "CanManageUsers")]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> InviteUser([FromServices] IIdentityHub hub)
    {
        var hasPermission = await hub.CheckPermissionAsync("users.invite");
        if (!hasPermission) return Forbid();

        // Your logic here
    }
}
```

---

## 📚 Documentation

- [Architecture Decision Records](docs/adr/README.md)
- [Graph API Permissions](docs/graph-permissions.md)
- [Multi-Tenancy Design](docs/multi-tenant.md)
- [Policy Engine Guide](docs/policy-engine.md)
- [Deployment Guide](docs/deployment.md)

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
