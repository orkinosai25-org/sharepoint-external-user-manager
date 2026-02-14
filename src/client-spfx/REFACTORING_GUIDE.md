# SPFx Refactoring Guide

## Overview

This document describes how SPFx web parts have been refactored to use the shared services layer introduced in CS-SAAS-REF-01.

## Architecture Changes

### Before Refactoring
```
React Component → SharePointDataService → PnPjs/Graph API
                → GraphApiService → Graph API  
                → BackendApiService → Backend API
```

### After Refactoring
```
React Component → Thin Service Wrapper → Shared ExternalUserService → SPFxGraphClient → Graph API
                                                                     → SPFxAuditService
```

## Benefits

1. **Code Reuse**: Business logic shared with backend API and future SaaS portal
2. **Consistency**: All platforms use the same logic for operations
3. **Testability**: Core business logic can be tested independently of SPFx
4. **Maintainability**: Changes to business logic only need to be made in one place
5. **Smaller Bundle**: Less duplicate code in SPFx packages

## Migration Pattern

### Old Pattern (Direct SharePoint Operations)
```typescript
// OLD: Business logic mixed with SPFx-specific code
export class SharePointDataService {
  private context: WebPartContext;
  private sp: any;

  constructor(context: WebPartContext) {
    this.context = context;
    this.sp = spfi().using(SPFx(context));
  }

  public async getExternalUsersForLibrary(libraryId: string): Promise<IExternalUser[]> {
    // 200+ lines of business logic mixed with PnPjs calls
    const roleAssignments = await this.sp.web.lists.getById(libraryId)
      .roleAssignments
      .expand("Member", "RoleDefinitionBindings")
      .get();
    
    // Complex logic for filtering, mapping, etc.
    ...
  }
}
```

### New Pattern (Thin Wrapper Over Shared Service)
```typescript
// NEW: Thin wrapper that delegates to shared service
import { ExternalUserService } from '../../../../services/core';
import { SPFxGraphClient, SPFxAuditService } from '../../../shared/adapters';
import { IExternalUser } from '../models/IExternalLibrary';

export class SharePointDataService {
  private userService: ExternalUserService;

  constructor(context: WebPartContext) {
    // Create adapters
    const graphClient = new SPFxGraphClient(context);
    const auditService = new SPFxAuditService(context);
    
    // Create shared service
    this.userService = new ExternalUserService(graphClient, auditService);
  }

  // Thin wrapper that converts between SPFx models and shared models
  public async getExternalUsersForLibrary(libraryUrl: string): Promise<IExternalUser[]> {
    // Delegate to shared service
    const users = await this.userService.listExternalUsers(libraryUrl);
    
    // Convert shared model to SPFx model if needed
    return users.map(user => ({
      id: user.id,
      email: user.email,
      displayName: user.displayName,
      invitedBy: user.invitedBy,
      invitedDate: user.invitedDate,
      lastAccess: user.lastAccess,
      permissions: user.permissions as any,
      company: user.metadata?.company,
      project: user.metadata?.project
    }));
  }
}
```

## Adapter Pattern for SPFx

### SPFxGraphClient
- Implements `IGraphClient` interface
- Uses SPFx's `MSGraphClientV3`
- Handles authentication via SPFx context

### SPFxAuditService
- Implements `IAuditService` interface
- Logs to console (can be extended to Application Insights)
- Uses SPFx correlation IDs

## File Organization

```
src/client-spfx/src/
├── shared/
│   ├── adapters/                    # NEW: SPFx adapters
│   │   ├── SPFxGraphClient.ts      # Graph API adapter
│   │   ├── SPFxAuditService.ts     # Audit logging adapter
│   │   └── index.ts
│   ├── services/
│   │   └── SaaSApiClient.ts        # Existing SaaS API client
│   └── components/
│       └── ...
└── webparts/
    └── externalUserManager/
        ├── services/
        │   ├── SharePointDataService.ts    # REFACTOR: Use shared service
        │   ├── GraphApiService.ts          # REFACTOR: Use shared service
        │   ├── BackendApiService.ts        # Keep as-is (calls SaaS API)
        │   └── MockDataService.ts          # Keep as-is
        ├── components/
        │   └── ExternalUserManager.tsx     # Minimal changes
        └── models/
            └── IExternalLibrary.ts         # Keep for backward compatibility
```

## Migration Steps

For each SPFx web part service:

1. **Import shared services and adapters**:
   ```typescript
   import { ExternalUserService, LibraryService } from '../../../../services/core';
   import { SPFxGraphClient, SPFxAuditService } from '../../../shared/adapters';
   ```

2. **Create adapters in constructor**:
   ```typescript
   constructor(context: WebPartContext) {
     const graphClient = new SPFxGraphClient(context);
     const auditService = new SPFxAuditService(context);
     this.userService = new ExternalUserService(graphClient, auditService);
   }
   ```

3. **Refactor methods to delegate to shared service**:
   ```typescript
   // Before
   public async addExternalUserToLibrary(libraryId: string, email: string, ...): Promise<void> {
     // 50+ lines of business logic
   }
   
   // After
   public async addExternalUserToLibrary(libraryUrl: string, email: string, ...): Promise<void> {
     const result = await this.userService.inviteUser({
       email,
       displayName,
       resourceUrl: libraryUrl,
       permission,
       metadata: { company, project }
     });
     
     if (!result.success) {
       throw new Error(result.error);
     }
   }
   ```

4. **Keep SPFx-specific features**:
   - PnPjs for operations not covered by shared services
   - SPFx context management
   - UI state management
   - Component lifecycle

## Example: Refactored SharePointDataService

See `src/client-spfx/src/webparts/externalUserManager/services/SharePointDataService.refactored.example.ts` for a complete example.

## Testing Strategy

### Unit Tests for Shared Services (Already Framework-Agnostic)
```typescript
import { ExternalUserService } from '../../../../services/core';
import { IGraphClient } from '../../../../services/interfaces';

const mockGraphClient: IGraphClient = {
  getAccessToken: async () => 'mock-token',
  request: async () => ({ /* mock data */ })
};

const service = new ExternalUserService(mockGraphClient);
const result = await service.inviteUser({...});
expect(result.success).toBe(true);
```

### Integration Tests for SPFx Wrappers
```typescript
import { SharePointDataService } from './SharePointDataService';
import { MockWebPartContext } from '@microsoft/sp-webpart-test';

const mockContext = new MockWebPartContext();
const service = new SharePointDataService(mockContext);
const users = await service.getExternalUsersForLibrary('mock-library-url');
expect(users).toBeArray();
```

## Rollout Plan

1. ✅ **Phase 1**: Create shared services layer
2. ✅ **Phase 2**: Create SPFx adapters
3. **Phase 3**: Refactor one service as proof of concept
4. **Phase 4**: Gradually migrate remaining services
5. **Phase 5**: Update components to use new service signatures
6. **Phase 6**: Remove old service implementations
7. **Phase 7**: Update tests

## Backward Compatibility

To ensure backward compatibility during migration:

1. **Keep existing model interfaces** (`IExternalLibrary`, `IExternalUser`)
2. **Convert between models** in wrapper services
3. **Maintain existing method signatures** where possible
4. **Add new methods** with `_v2` suffix if signatures must change
5. **Deprecate old methods** with `@deprecated` comments

## Notes

- SPFx services remain as thin wrappers
- Business logic moves to shared layer
- Graph API operations use shared services
- PnPjs operations can remain in SPFx for now
- React components require minimal changes
- Bundle size should decrease (less duplicate code)

## Migration Checklist Per Service

- [ ] Create adapter instances in constructor
- [ ] Identify methods that use Graph API directly
- [ ] Replace with shared service calls
- [ ] Add model conversion if needed
- [ ] Update method signatures if necessary
- [ ] Add deprecation notices for old methods
- [ ] Test with existing components
- [ ] Update documentation
