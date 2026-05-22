#!/bin/bash

# Simple code analysis script for FinanceApp Backend
# Uses the linting setup from Directory.Build.props and .editorconfig

set -e

SOLUTION_PATH="IdentityHub.sln"

echo "🔍 Running code analysis for IdentityHub Backend..."

# Check if solution exists
if [[ ! -f "$SOLUTION_PATH" ]]; then
    echo "❌ Solution file not found: $SOLUTION_PATH"
    exit 1
fi

# 1. Restore packages
echo "📦 Restoring packages..."
dotnet restore "$SOLUTION_PATH"

# 2. Build with analysis enabled
echo "🔨 Building with code analysis..."
dotnet build "$SOLUTION_PATH" --no-restore --verbosity normal

# 3. Format check (optional - shows what would be changed)
echo "🎨 Checking code formatting..."

# 3. Format check (fail if issues found)
echo "🎨 Checking code formatting..."
dotnet format "$SOLUTION_PATH" --verify-no-changes --verbosity normal
if [[ $? -ne 0 ]]; then
    echo "❌ Code formatting issues found. Run 'dotnet format' to fix them."
    exit 1
else
    echo "✅ Code formatting is good!"
fi

echo "✨ Code analysis complete!"
