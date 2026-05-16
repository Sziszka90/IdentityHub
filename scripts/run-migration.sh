#!/usr/bin/env bash

PROVIDER="$1"

CONTEXT="IdentityHubDbContext"
PROJECT="IdentityHub.Infrastructure"

echo "📦 Applying migrations for '$PROVIDER'..."

# Restore NuGet packages first
echo "🔄 Restoring NuGet packages..."
dotnet restore


dotnet ef database update \
    --project "$PROJECT" \
    --startup-project IdentityHub.Application \
    --context "$CONTEXT"
