#!/bin/bash

# Script to generate OpenAPI specification
# This script runs the API temporarily and extracts the OpenAPI JSON

echo "Generating OpenAPI specification for SharePoint External User Manager API..."

cd "$(dirname "$0")"
cd ../SharePointExternalUserManager.Api

# Build the project
echo "Building the API project..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "Build failed. Please fix build errors first."
    exit 1
fi

# Check if dotnet swagger tool is installed
if ! dotnet tool list -g | grep -q "Swashbuckle.AspNetCore.Cli"; then
    echo "Installing Swashbuckle CLI tool..."
    dotnet tool install -g Swashbuckle.AspNetCore.Cli
fi

# Generate the OpenAPI spec using Swashbuckle CLI
echo "Generating OpenAPI spec..."
dotnet swagger tofile --output ../../openapi.json bin/Release/net8.0/SharePointExternalUserManager.Api.dll v1

if [ $? -eq 0 ]; then
    echo "✅ OpenAPI specification generated successfully: openapi.json"
    echo "You can view it at: $(pwd)/../../openapi.json"
else
    echo "❌ Failed to generate OpenAPI specification"
    exit 1
fi
