#!/bin/bash

# Azure Functions Documentation Generator
# This script generates Markdown documentation for all Azure Functions

echo "🔧 Azure Functions Documentation Generator"
echo "=========================================="
echo

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"
DOC_GEN_DIR="$PROJECT_ROOT/Utils/DocumentationGenerator"

echo "📁 Project root: $PROJECT_ROOT"
echo "📁 Documentation generator: $DOC_GEN_DIR"

# Check if the documentation generator exists
if [ ! -f "$DOC_GEN_DIR/DocumentationGenerator.csproj" ]; then
    echo "❌ Documentation generator project not found"
    echo "Expected location: $DOC_GEN_DIR/DocumentationGenerator.csproj"
    exit 1
fi

echo "🔨 Building documentation generator..."
cd "$DOC_GEN_DIR"
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Failed to build documentation generator"
    exit 1
fi

echo "🚀 Running documentation generator..."
dotnet run

echo "✅ Documentation generation complete!"
echo "📂 Check the docs/functions directory for generated files"