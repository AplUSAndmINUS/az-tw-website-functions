#!/bin/bash

# Example script to run the CreateAuthor integration test
# Save this as run-integration-test.sh and make it executable with: chmod +x run-integration-test.sh

echo "Setting up environment variables for DEV integration test..."

# Set your DEV environment variables here
# IMPORTANT: Replace these with your actual DEV values
export StorageAccountName="your-dev-storage-account-name"
export X_API_ENVIRONMENT_KEY="your-dev-api-key"
export AUTHORS_TABLE_NAME="authors"  # Optional, defaults to "authors"

echo "Environment configured:"
echo "  StorageAccountName: $StorageAccountName"
echo "  X_API_ENVIRONMENT_KEY: ${X_API_ENVIRONMENT_KEY:0:8}..." # Show only first 8 chars for security
echo "  AUTHORS_TABLE_NAME: $AUTHORS_TABLE_NAME"
echo ""

# Navigate to the test directory and run the integration test
cd /Users/terencewaters/source/repos/az-tw-website-functions/Tests

echo "Building test project..."
dotnet build

if [ $? -eq 0 ]; then
    echo ""
    echo "Running CreateAuthor integration test..."
    dotnet run
else
    echo "Build failed. Please fix compilation errors first."
    exit 1
fi
