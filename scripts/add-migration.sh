#!/bin/bash

MIGRATION_NAME=$1
PROVIDER=$2

if [ -z "$MIGRATION_NAME" ]; then
  echo "❌ Migration name is required."
  echo "Usage: ./scripts/add-migration.sh MigrationName"
  exit 1
fi

# Configure values based on provider
PROJECT="IdentityHub.Infrastructure"
CONTEXT="IdentityHubDbContext";


echo "✅ Adding migration "$MIGRATION_NAME" for "$PROVIDER...""

dotnet ef migrations add "$MIGRATION_NAME" \
  --project "$PROJECT" \
  --startup-project "IdentityHub.Application" \
  --context "$CONTEXT"
