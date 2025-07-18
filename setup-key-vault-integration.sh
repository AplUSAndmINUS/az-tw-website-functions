#!/bin/bash

# Azure Function App Key Vault Integration Setup Script
# This script configures the Function Apps to use Key Vault for API keys

set -e

echo "🔧 Azure Functions + Key Vault Integration Setup"
echo "================================================"

# CONFIGURATION: Replace these placeholders with your actual values
# Configuration
KEY_VAULT_NAME="{{KEY-VAULT-NAME}}"
KEY_VAULT_RESOURCE_GROUP="{{KEY-VAULT-RESOURCE-GROUP}}"
FUNCTIONS_RESOURCE_GROUP="{{FUNCTIONS-RESOURCE-GROUP}}"
SUBSCRIPTION_ID="{{SUBSCRIPTION-ID}}"

# Function App configurations
declare -A FUNCTION_APPS=(
    ["{{PROD-FUNCTION-APP}}"]="{{PROD-MANAGED-IDENTITY-ID}}"
    ["{{DEV-FUNCTION-APP}}"]="{{DEV-MANAGED-IDENTITY-ID}}"
    ["{{TEST-FUNCTION-APP}}"]="{{TEST-MANAGED-IDENTITY-ID}}"
)

# Function to check if user is logged in
check_azure_login() {
    echo "🔍 Checking Azure CLI login status..."
    if ! az account show >/dev/null 2>&1; then
        echo "❌ Not logged in to Azure. Please run 'az login' first."
        exit 1
    fi
    echo "✅ Azure CLI login verified"
}

# Function to enable managed identity for Function App
enable_managed_identity() {
    local function_app_name=$1
    local expected_principal_id=$2
    
    echo "🔐 Enabling managed identity for $function_app_name..."
    
    # Enable system-assigned managed identity
    az functionapp identity assign \
        --resource-group $FUNCTIONS_RESOURCE_GROUP \
        --name $function_app_name \
        --output none
    
    # Get the principal ID
    PRINCIPAL_ID=$(az functionapp identity show \
        --resource-group $FUNCTIONS_RESOURCE_GROUP \
        --name $function_app_name \
        --query principalId -o tsv)
    
    echo "✅ Function App Principal ID: $PRINCIPAL_ID"
    
    # Verify it matches expected (if provided)
    if [[ -n "$expected_principal_id" && "$PRINCIPAL_ID" != "$expected_principal_id" ]]; then
        echo "⚠️  Warning: Principal ID ($PRINCIPAL_ID) doesn't match expected ($expected_principal_id)"
    fi
    
    return 0
}

# Function to grant Key Vault access
grant_key_vault_access() {
    local function_app_name=$1
    local principal_id=$2
    
    echo "🔑 Granting Key Vault access to $function_app_name..."
    
    # Grant Key Vault Secrets User role
    az role assignment create \
        --role "Key Vault Secrets User" \
        --assignee $principal_id \
        --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$KEY_VAULT_RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME" \
        --output none
    
    echo "✅ Key Vault access granted"
}

# Function to verify Key Vault secrets exist
verify_key_vault_secrets() {
    echo "🔍 Verifying Key Vault secrets exist..."
    
    local secrets=("{{DEV-SECRET-NAME}}" "{{STAGING-SECRET-NAME}}" "{{PROD-SECRET-NAME}}")
    
    for secret in "${secrets[@]}"; do
        if az keyvault secret show --vault-name $KEY_VAULT_NAME --name $secret >/dev/null 2>&1; then
            echo "✅ Secret '$secret' exists"
        else
            echo "❌ Secret '$secret' does not exist"
            echo "   Please create it using: az keyvault secret set --vault-name $KEY_VAULT_NAME --name $secret --value 'your-api-key'"
        fi
    done
}

# Function to test Key Vault access
test_key_vault_access() {
    local function_app_name=$1
    local secret_name=$2
    
    echo "🧪 Testing Key Vault access for $function_app_name..."
    
    # This is a basic test - in production, you'd test from within the Function App
    if az keyvault secret show --vault-name $KEY_VAULT_NAME --name $secret_name >/dev/null 2>&1; then
        echo "✅ Can access secret '$secret_name' from current context"
    else
        echo "❌ Cannot access secret '$secret_name'"
    fi
}

# Main setup function
setup_key_vault_integration() {
    echo "🚀 Starting Key Vault integration setup..."
    
    # Check prerequisites
    check_azure_login
    verify_key_vault_secrets
    
    # Setup each Function App
    for function_app in "${!FUNCTION_APPS[@]}"; do
        echo ""
        echo "🔧 Setting up $function_app..."
        
        expected_principal_id="${FUNCTION_APPS[$function_app]}"
        
        # Enable managed identity
        enable_managed_identity "$function_app" "$expected_principal_id"
        
        # Get the actual principal ID
        PRINCIPAL_ID=$(az functionapp identity show \
            --resource-group $FUNCTIONS_RESOURCE_GROUP \
            --name $function_app \
            --query principalId -o tsv)
        
        # Grant Key Vault access
        grant_key_vault_access "$function_app" "$PRINCIPAL_ID"
        
        # Determine which secret to test with
        case $function_app in
            "{{PROD-FUNCTION-APP}}")
                secret_name="{{PROD-SECRET-NAME}}"
                ;;
            "{{DEV-FUNCTION-APP}}")
                secret_name="{{DEV-SECRET-NAME}}"
                ;;
            "{{TEST-FUNCTION-APP}}")
                secret_name="{{STAGING-SECRET-NAME}}"
                ;;
        esac
        
        # Test access
        test_key_vault_access "$function_app" "$secret_name"
        
        echo "✅ $function_app setup complete"
    done
}

# Function to display summary
display_summary() {
    echo ""
    echo "📋 Setup Summary"
    echo "================"
    echo "Key Vault: $KEY_VAULT_NAME"
    echo "Resource Group: $KEY_VAULT_RESOURCE_GROUP"
    echo ""
    echo "Function Apps configured:"
    for function_app in "${!FUNCTION_APPS[@]}"; do
        echo "  - $function_app (${FUNCTION_APPS[$function_app]})"
    done
    echo ""
    echo "🎉 Key Vault integration setup complete!"
    echo ""
    echo "Next steps:"
    echo "1. Deploy your Function Apps with the updated code"
    echo "2. Test the API endpoints to verify Key Vault integration"
    echo "3. Monitor Application Insights for any Key Vault-related errors"
}

# Script execution
main() {
    setup_key_vault_integration
    display_summary
}

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
