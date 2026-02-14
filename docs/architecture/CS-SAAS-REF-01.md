# CS-SAAS-REF-01: Shared Services Architecture

## Overview

This document describes the architecture changes implemented in CS-SAAS-REF-01 to separate SaaS core business logic from SPFx UI layer, enabling a SaaS portal-first architecture.

## Problem Statement

**Before**: Business logic was tightly coupled with SPFx web parts
- SPFx `SharePointDataService` contained 700+ lines of business logic
- API backend had duplicate Graph API logic
- No way to reuse logic in SaaS portal
- Changes required updates in multiple places

**After**: Business logic is in a shared, framework-agnostic layer
- Core services contain business logic
- SPFx, API, and Portal all use same services
- Single source of truth for operations
- Easy to test and maintain

## Architecture Layers

### 1. Shared Services Layer (`/src/services`)

**Framework-agnostic TypeScript services**

```
/src/services/
├── models/              # Domain models (ExternalUser, SharePointLibrary, etc.)
├── interfaces/          # Service contracts (IExternalUserService, etc.)
├── core/               # Service implementations
│   ├── ExternalUserService.ts
│   └── LibraryService.ts
└── index.ts            # Public API
```

**Key Principles:**
- No dependencies on SPFx, Express, or any framework
- Uses interface-based design (`IGraphClient`, `IAuditService`)
- Async/await throughout
- Full TypeScript with strict mode

### 2. Adapter Layer

**Platform-specific implementations of interfaces**

#### SPFx Adapters (`/src/client-spfx/src/shared/adapters`)
```typescript
SPFxGraphClient    implements IGraphClient
  ↓ Uses MSGraphClientV3
  ↓ Uses SPFx authentication

SPFxAuditService   implements IAuditService
  ↓ Logs to console
  ↓ Can extend to Application Insights
```

#### Backend API Adapters (`/src/api-dotnet/src/adapters`)
```typescript
BackendGraphClient implements IGraphClient
  ↓ Uses Azure Identity (ClientSecretCredential)
  ↓ Uses Microsoft Graph Client

BackendAuditService implements IAuditService
  ↓ Uses existing auditLogger
  ↓ Stores to database
```

### 3. Service Wrapper Layer

**Thin wrappers that bridge platform models to shared models**

## Data Flow

### External User Invitation

**SPFx:**
```
React Component → SPFx Wrapper → ExternalUserService → SPFxGraphClient → Graph API
```

**API Backend:**
```
HTTP Handler → Middleware → ExternalUserService → BackendGraphClient → Graph API
```

## Benefits Achieved

1. **Code Reusability**: Same logic in SPFx, API, and future portal
2. **Maintainability**: Single source of truth for business logic
3. **Testability**: Unit tests without framework dependencies
4. **Flexibility**: Easy to swap implementations
5. **SaaS Portal Ready**: Services ready for Blazor/React portal

## File Structure

```
sharepoint-external-user-manager/
├── src/
│   ├── services/                    # NEW: Shared services
│   ├── client-spfx/
│   │   └── src/shared/adapters/     # NEW: SPFx adapters
│   └── api-dotnet/
│       └── src/adapters/            # NEW: Backend adapters
```

## Migration Strategy

- ✅ Phase 1: Create shared services
- ✅ Phase 2: Create backend adapters
- ✅ Phase 3: Create SPFx adapters
- ⏳ Phase 4: Gradual migration
- ⏳ Phase 5: Deprecate old code

See `REFACTORING_GUIDE.md` in `client-spfx` and `api-dotnet` for detailed migration instructions.
