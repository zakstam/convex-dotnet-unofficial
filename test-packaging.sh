#!/bin/bash
# Local packaging test script
# This simulates what the GitHub Actions workflow does

VERSION="${1:-1.0.5-alpha}"

echo "🧪 Testing NuGet Package Creation Locally"
echo "Version: $VERSION"
echo ""

# Create output directory
OUTPUT_DIR="./test-nupkg"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

echo "📦 Step 1: Restoring dependencies..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Restore failed!"
    exit 1
fi

echo "🔨 Step 2: Building solution..."
dotnet build --configuration Release -p:Version="$VERSION" -p:PackageVersion="$VERSION"
if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo "🧪 Step 3: Running tests..."
dotnet test --configuration Release --verbosity normal --no-build
TEST_EXIT_CODE=$?
if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo "⚠️  Tests failed, but continuing with packaging test..."
fi

echo "📦 Step 4: Packing packages..."
dotnet pack --no-build --configuration Release --output "$OUTPUT_DIR" -p:Version="$VERSION" -p:PackageVersion="$VERSION"
PACK_EXIT_CODE=$?

echo ""
echo "📊 Results:"
PACKAGE_COUNT=$(find "$OUTPUT_DIR" -name "*.nupkg" ! -name "*.snupkg" | wc -l)
SYMBOL_COUNT=$(find "$OUTPUT_DIR" -name "*.snupkg" | wc -l)

echo "  Packages created: $PACKAGE_COUNT"
echo "  Symbol packages: $SYMBOL_COUNT"

if [ $PACKAGE_COUNT -gt 0 ]; then
    echo ""
    echo "✅ Created packages:"
    find "$OUTPUT_DIR" -name "*.nupkg" ! -name "*.snupkg" -exec basename {} \; | while read pkg; do
        SIZE=$(du -h "$OUTPUT_DIR/$pkg" | cut -f1)
        echo "    - $pkg ($SIZE)"
    done
fi

echo ""
if [ $PACK_EXIT_CODE -eq 0 ]; then
    echo "✅ Packaging test completed successfully!"
    echo "   Packages are in: $OUTPUT_DIR"
else
    echo "❌ Packaging failed with exit code: $PACK_EXIT_CODE"
    echo "   Check the errors above for details"
    exit $PACK_EXIT_CODE
fi

