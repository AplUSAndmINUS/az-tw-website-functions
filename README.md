# Azure Website Functions

This repository contains Azure Functions that power the backend services for TerenceWaters.com.

## Overview

This project is a collection of Azure Functions written in .NET 8 that provide APIs for managing website content including blog posts, authors, and media assets. The functions use Azure Blob Storage for file storage and Azure Table Storage for structured data.

## Project Structure

- **src/Functions/** - Contains all Azure Functions endpoints organized by domain
  - **Authors/** - Author management functions
  - **BlogPosts/** - Blog post management functions
- **SharedStorage/** - Shared storage services for Azure Blob and Table Storage
- **Utils/** - Utility classes, validators, and helpers

## Features

- RESTful API endpoints for content management
- Blob storage integration for media files
- Table storage for structured data
- Image conversion and thumbnail generation
- API key authentication
- Application Insights integration for monitoring

## Getting Started

### Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Account (or Azurite for local development)
- Visual Studio Code or Visual Studio 2022

### Local Development

1. Clone this repository
2. Configure your `local.settings.json` file:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=false",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_CREDENTIALS": "your-azure-credentials-JSON",
    "StorageAccountName": "your-storage-account-name",
    "X_API_ENVIRONMENT_KEY": "your-api-key"
  }
}