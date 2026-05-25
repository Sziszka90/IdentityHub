#!/usr/bin/env bash

# Resolve repository root so the script works from any current directory.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PROJECT="$REPO_ROOT/src/IdentityHub.Infrastructure/IdentityHub.Infrastructure.csproj"
STARTUP_PROJECT="$REPO_ROOT/src/IdentityHub.API/IdentityHub.API.csproj"
CONTEXT="IdentityHubDbContext"

echo "📦 Applying migrations for MSSQL..."

# Restore NuGet packages first
echo "🔄 Restoring NuGet packages..."
dotnet restore "$PROJECT"

dotnet ef database update \
    --project "$PROJECT" \
    --startup-project "$STARTUP_PROJECT" \
    --context "$CONTEXT"
