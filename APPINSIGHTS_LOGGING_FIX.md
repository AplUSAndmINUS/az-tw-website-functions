# AppInsights Logging Fix Documentation

## Issue Summary
AppInsights logging stopped working in DEV and TEST environments since 7/23, while continuing to work in PROD. This was likely due to missing or misconfigured Application Insights environment variables in the DEV/TEST environments.

## Root Cause
Azure Functions requires one of the following environment variables to be set for Application Insights to work properly:
- `APPLICATIONINSIGHTS_CONNECTION_STRING` (recommended)
- `APPINSIGHTS_INSTRUMENTATIONKEY` (legacy)

The DEV and TEST environments were missing these critical configuration values.

## Changes Made

### 1. Enhanced AppInsightsLogger.cs
- Added resilient error handling for when TelemetryClient is not properly configured
- Added configuration checking to determine if AppInsights is properly set up
- Modified all logging methods to gracefully fall back to standard logging when telemetry fails
- Added telemetry configuration validation with clear warning messages

### 2. Enhanced Program.cs
- Added startup diagnostics to check for missing AppInsights configuration
- Provides clear console warnings when configuration is missing
- Includes guidance on how to fix the configuration issue

### 3. Added DiagnosticsFunction.cs
- New diagnostic endpoint: `/diagnostics/appinsights`
- Provides detailed information about AppInsights configuration status
- Shows environment variables and TelemetryClient status
- Can be used to troubleshoot configuration issues in deployed environments

## Solution for DEV/TEST Environments

To fix the AppInsights logging issue, set the following environment variable in your Azure Function App settings:

```
APPLICATIONINSIGHTS_CONNECTION_STRING = <Your App Insights Connection String>
```

### Where to find the connection string:
1. Go to your Application Insights resource in Azure Portal
2. Navigate to "Overview" or "Properties"
3. Copy the "Connection String" value
4. Add it to your Function App's "Configuration" > "Application settings"

### Alternative (Legacy):
If using the older instrumentation key approach:
```
APPINSIGHTS_INSTRUMENTATIONKEY = <Your App Insights Instrumentation Key>
```

## Verification

### During Deployment:
- Check console output during function startup for AppInsights configuration messages
- Look for warnings about missing configuration

### After Deployment:
- Call the diagnostic endpoint: `GET /diagnostics/appinsights` (requires function-level authorization)
- Check the response for configuration status

### Example Diagnostic Response:
```json
{
  "Environment": "develop", 
  "AppInsightsConfiguration": {
    "ConnectionString": "Set",
    "InstrumentationKey": "Not Set",
    "TelemetryClientConfigured": true,
    "TelemetryClientInstrumentationKey": "12345678-1234-1234-1234-123456789abc"
  },
  "FunctionAppSettings": {
    "WebsiteSiteName": "az-tw-website-functions-dev",
    "AzureWebJobsStorage": "Set",
    "KeyVaultUri": "https://kv-tw-website-vault.vault.azure.net/"
  },
  "Timestamp": "2024-07-31T23:51:00.000Z"
}
```

## Benefits of This Fix

1. **Resilience**: Functions continue to work even when AppInsights is misconfigured
2. **Diagnostics**: Easy to identify and troubleshoot configuration issues
3. **Graceful Degradation**: Standard logging continues to work when telemetry fails
4. **Clear Guidance**: Startup messages provide actionable steps to fix issues

## Testing

The fix has been tested with:
- Successful compilation and build
- Verification of enhanced error handling
- Confirmation of diagnostic endpoint creation
- Validation of startup diagnostic messages

## Next Steps

1. Deploy the updated code to DEV and TEST environments
2. Configure the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable
3. Verify logging is working using the diagnostic endpoint
4. Monitor AppInsights data to confirm telemetry is flowing correctly

## Rollback Plan

If issues arise, the changes are minimal and backward-compatible:
- The AppInsightsLogger still uses the same interface
- Standard logging continues to work regardless of AppInsights configuration
- No breaking changes to existing functionality