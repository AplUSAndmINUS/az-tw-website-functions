#!/bin/bash

# Script to verify role assignments for managed identities on the storage account
echo "🔍 Verifying storage account role assignments"

# Variables (replace with your values)
STORAGE_ACCOUNT_NAME="aztwwebsitestorage"
RESOURCE_GROUP="az-tw-website-functions"
DEVELOP_CLIENT_ID="{{your-develop-managed-identity-client-id}}"
TEST_CLIENT_ID="{{your-test-managed-identity-client-id}}"
PRODUCTION_CLIENT_ID="{{your-production-managed-identity-client-id}}"

# Get the storage account ID
echo "📝 Getting storage account ID..."
STORAGE_ID=$(az storage account show --name $STORAGE_ACCOUNT_NAME --query id --output tsv)
echo "Storage Account ID: $STORAGE_ID"

# Check role assignments for develop
echo -e "\n🔍 Checking role assignments for develop (client ID: $DEVELOP_CLIENT_ID)..."
az role assignment list --assignee $DEVELOP_CLIENT_ID --scope $STORAGE_ID --query "[].{roleDefinitionName:roleDefinitionName, principalType:principalType, scope:scope}" --output table

# Check role assignments for test
echo -e "\n🔍 Checking role assignments for test (client ID: $TEST_CLIENT_ID)..."
az role assignment list --assignee $TEST_CLIENT_ID --scope $STORAGE_ID --query "[].{roleDefinitionName:roleDefinitionName, principalType:principalType, scope:scope}" --output table

# Check role assignments for production
echo -e "\n🔍 Checking role assignments for production (client ID: $PRODUCTION_CLIENT_ID)..."
az role assignment list --assignee $PRODUCTION_CLIENT_ID --scope $STORAGE_ID --query "[].{roleDefinitionName:roleDefinitionName, principalType:principalType, scope:scope}" --output table

# Check if the expected roles are assigned
echo -e "\n📋 Expected roles for full access:"
echo "- Storage Blob Data Contributor"
echo "- Storage Table Data Contributor" 
echo "- Storage Queue Data Contributor"
echo "- Monitoring Publisher (optional)"

# Display role definitions for reference
echo -e "\n📝 Reference: Role definition IDs for storage access:"
echo "- Storage Blob Data Contributor: {{REDACTED-GUID}}"
echo "- Storage Table Data Contributor: {{REDACTED-GUID}}"
echo "- Storage Queue Data Contributor: {{REDACTED-GUID}}"
echo "- Monitoring Publisher: {{REDACTED-GUID}}"
