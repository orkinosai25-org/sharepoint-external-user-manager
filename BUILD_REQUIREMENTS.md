# Build Requirements and Known Issues

## Node.js Version Requirement

### SPFx Client (`/src/client-spfx`)

**Required Node Version**: 16.x or 18.x

The SPFx client requires Node.js version 16.13.0+ or 18.17.0+ (but less than 19.0.0) to build properly. 

**Current Environment**: Node.js 24.x is installed in the CI/CD environment, which is incompatible with SPFx 1.18.2.

### Solution

To build the SPFx client locally:

1. **Using nvm (recommended)**:
   ```bash
   # Install Node 18 LTS
   nvm install 18
   
   # Switch to Node 18
   nvm use 18
   
   # Navigate to client directory
   cd src/client-spfx
   
   # Install dependencies
   npm install
   
   # Build
   npm run build
   ```

2. **Using Docker**:
   ```bash
   # Use Node 18 container
   docker run -it --rm -v $(pwd):/workspace -w /workspace/src/client-spfx node:18 bash
   npm install
   npm run build
   ```

### CI/CD Integration

For GitHub Actions, add a step to use the correct Node version:

```yaml
- uses: actions/setup-node@v3
  with:
    node-version: '18'
```

## Backend API (`/src/api-dotnet`)

**Current Implementation**: Node.js 18+ (Azure Functions v4)

**Target Implementation**: .NET 8 (Coming in ISSUE-02)

The backend currently uses Node.js but will be migrated to .NET 8 in a future issue.

## Status

- ✅ Repository structure reorganized
- ✅ All files moved to correct locations
- ⚠️ SPFx build requires Node 18.x environment (not available in current CI/CD runner)
- ✅ Documentation updated

## Next Steps

1. Update CI/CD workflows to use Node 18 for SPFx builds (ISSUE-11)
2. Migrate backend to .NET 8 (ISSUE-02)
3. Create Blazor portal (ISSUE-08)
