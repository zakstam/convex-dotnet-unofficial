#!/usr/bin/env bash
# Build script for Convex .NET Client

set -e

TARGET="${1:-all}"
CONFIGURATION="${2:-Debug}"
CLEAN="${3:-false}"

echo "🔨 Convex .NET Client Build Script"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
SOLUTION_FILE="$ROOT_DIR/convex-dotnet-client.sln"

# Clean if requested
if [ "$CLEAN" = "true" ] || [ "$CLEAN" = "clean" ]; then
    echo "🧹 Cleaning solution..."
    dotnet clean "$SOLUTION_FILE" -c "$CONFIGURATION"
fi

# Build target
if [ "$TARGET" = "all" ]; then
    echo "📦 Building entire solution ($CONFIGURATION)..."
    dotnet build "$SOLUTION_FILE" -c "$CONFIGURATION"
else
    echo "📦 Building $TARGET ($CONFIGURATION)..."
    PROJECT_PATH=$(find "$ROOT_DIR" -name "$TARGET.csproj" -type f | head -n 1)
    if [ -n "$PROJECT_PATH" ]; then
        dotnet build "$PROJECT_PATH" -c "$CONFIGURATION"
    else
        echo "❌ Project '$TARGET' not found"
        exit 1
    fi
fi

echo ""
echo "✅ Build completed successfully!"
