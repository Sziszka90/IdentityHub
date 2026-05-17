#!/bin/bash

MIGRATION_NAME=$1

if [ -z "$MIGRATION_NAME" ]; then
  echo "❌ Migration name is required."
  echo "Usage: ./scripts/add-migration.sh MigrationName"
  exit 1
fi

# Resolve repository root so the script works from any current directory.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PROJECT="$REPO_ROOT/src/IdentityHub.Infrastructure/IdentityHub.Infrastructure.csproj"
STARTUP_PROJECT="$REPO_ROOT/src/IdentityHub.Infrastructure/IdentityHub.Infrastructure.csproj"
CONTEXT="IdentityHubDbContext"


echo "✅ Adding migration "$MIGRATION_NAME" for mssql"

dotnet ef migrations add "$MIGRATION_NAME" \
  --project "$PROJECT" \
  --startup-project "$STARTUP_PROJECT" \
  --context "$CONTEXT"
