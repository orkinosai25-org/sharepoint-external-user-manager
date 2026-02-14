# API Backend Refactoring Guide

## Overview

This document describes how the API backend has been refactored to use the shared services layer introduced in CS-SAAS-REF-01.

## Architecture Changes

### Before Refactoring
```
API Function → graphClient.ts → Microsoft Graph API
```

### After Refactoring
```
API Function → Shared ExternalUserService → BackendGraphClient → Microsoft Graph API
                                              BackendAuditService
```

## Benefits

1. **Code Reuse**: Business logic is now shared between SPFx, API, and future SaaS portal
2. **Consistency**: All platforms use the same logic for external user management
3. **Testability**: Core business logic can be tested independently
4. **Maintainability**: Changes to business logic only need to be made in one place

## Migration Pattern

### Old Pattern (Direct Graph Client)
```typescript
// OLD: Direct call to graphClient
const user = await graphClient.inviteExternalUser(
  email,
  displayName,
  library,
  permissions,
  message
);
```

### New Pattern (Shared Service)
```typescript
// NEW: Use shared service with adapters
import { ExternalUserService } from '../../../services/core';
import { BackendGraphClient, BackendAuditService } from '../adapters';

// Create service instance (can be singleton)
const graphClient = new BackendGraphClient();
const auditService = new BackendAuditService();
const userService = new ExternalUserService(graphClient, auditService);

// Use service
const result = await userService.inviteUser({
  email,
  displayName,
  resourceUrl: library,
  permission: permissions,
  message,
  metadata
});
```

## Adapter Pattern

The backend uses **adapters** to bridge the shared services layer with backend-specific implementations:

### BackendGraphClient
- Implements `IGraphClient` interface
- Uses Azure Identity for authentication
- Wraps Microsoft Graph Client

### BackendAuditService
- Implements `IAuditService` interface
- Bridges to existing `auditLogger` service
- Provides consistent logging interface

## File Organization

```
src/api-dotnet/src/
├── adapters/                   # NEW: Adapters for shared services
│   ├── BackendGraphClient.ts  # Graph API adapter
│   ├── BackendAuditService.ts # Audit logging adapter
│   └── index.ts
├── services/                   # EXISTING: Backend-specific services
│   ├── graphClient.ts         # TO BE DEPRECATED: Use shared service instead
│   ├── sharePointService.ts   # TO BE DEPRECATED: Use shared service instead
│   └── ...
└── functions/                  # API endpoints
    └── users/                  # External user endpoints
        ├── inviteUser.ts      # Uses shared ExternalUserService
        ├── listUsers.ts       # Uses shared ExternalUserService
        └── removeUser.ts      # Uses shared ExternalUserService
```

## Migration Steps

For each API function that deals with external users, SharePoint sites, or libraries:

1. **Import shared services**:
   ```typescript
   import { ExternalUserService, LibraryService } from '../../../services/core';
   import { BackendGraphClient, BackendAuditService } from '../../adapters';
   ```

2. **Create service instances** (ideally as singletons):
   ```typescript
   const graphClient = new BackendGraphClient();
   const auditService = new BackendAuditService();
   const userService = new ExternalUserService(graphClient, auditService);
   ```

3. **Replace direct Graph API calls** with service methods:
   ```typescript
   // Before
   const user = await graphClient.inviteExternalUser(...);
   
   // After
   const result = await userService.inviteUser({...});
   if (result.success && result.user) {
     const user = result.user;
   }
   ```

4. **Keep middleware and validation** as-is:
   - Authentication (`authenticateRequest`)
   - Authorization (`requirePermission`)
   - Subscription enforcement (`enforceSubscription`)
   - Rate limiting
   - Input validation

## Example: Refactored inviteUser Function

See `src/api-dotnet/src/functions/users/inviteUser.refactored.ts` for a complete example of a refactored function using the shared services layer.

## Testing

The shared services can be tested independently with mock Graph clients:

```typescript
import { ExternalUserService } from '../../../services/core';
import { IGraphClient } from '../../../services/interfaces';

// Create mock client
const mockGraphClient: IGraphClient = {
  getAccessToken: async () => 'mock-token',
  request: async (endpoint, method, body) => {
    // Return mock data
  }
};

// Test service
const service = new ExternalUserService(mockGraphClient);
const result = await service.inviteUser({...});
expect(result.success).toBe(true);
```

## Rollout Plan

1. ✅ **Phase 1**: Create shared services layer
2. ✅ **Phase 2**: Create backend adapters
3. **Phase 3**: Refactor one function as proof of concept
4. **Phase 4**: Gradually migrate remaining functions
5. **Phase 5**: Deprecate old service implementations
6. **Phase 6**: Update documentation and tests

## Notes

- The shared services layer is framework-agnostic TypeScript
- Works with both Node.js (API backend) and browser (SPFx)
- All services use async/await for consistency
- Error handling is built into services
- Audit logging is optional but recommended
