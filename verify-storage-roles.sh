#!/bin/bash

# Script to verify role assignments for managed identities on the storage account
echo "🔍 Verifying storage account role assignments"

# Variables (replace with your values)
STORAGE_ACCOUNT_NAME="aztwwebsitestorage"
RESOURCE_GROUP="az-tw-website-functions"
DEVELOP_CLIENT_ID="6c142dd0-8c6e-44ea-8cb8-c256ddc2fdf9"
TEST_CLIENT_ID="692a5d44-8bed-4d4e-94b4-5c19f02888ab"
PRODUCTION_CLIENT_ID="5b693580-ba39-4221-b2fa-f42ec029d465"

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
echo "- Storage Blob Data Contributor: ba92f5b4-2d11-453d-a403-e96b0029c9fe"
echo "- Storage Table Data Contributor: 0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3"
echo "- Storage Queue Data Contributor: 974c5e8b-45b9-4653-ba55-5f855dd0fb88"
echo "- Monitoring Publisher: 3913510d-42f4-4e42-8a64-420c390055eb"
