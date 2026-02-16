# Shared Services Layer

This directory contains the core business logic and service layer that is shared between:
- SaaS Portal (Blazor)
- SPFx Web Parts
- Backend API

## Structure

```
/services
├── core/               # Core service implementations
├── interfaces/         # Service interfaces and contracts
├── implementations/    # Concrete implementations
├── utils/             # Utility functions and helpers
└── README.md          # This file
```

## Purpose

The shared services layer provides:
1. **Business Logic Separation**: Core business logic is decoupled from UI frameworks (SPFx, Blazor)
2. **Reusability**: Services can be used by both SaaS portal and SPFx web parts
3. **Maintainability**: Changes to business logic are made in one place
4. **Testability**: Services can be tested independently of UI

## Key Services

### External User Management
- **IExternalUserService**: Interface for managing external users
- Operations: invite, remove, list, update permissions

### Site and Library Management
- **ISiteProvisioningService**: Interface for provisioning SharePoint sites
- **ILibraryService**: Interface for managing document libraries

### Permission Management
- **IPermissionService**: Interface for managing SharePoint permissions
- Operations: grant, revoke, check permissions

## Design Principles

1. **Framework Agnostic**: Services should not depend on SPFx or Blazor-specific types
2. **Interface-Based**: All services implement interfaces for easy mocking and testing
3. **Dependency Injection**: Services use constructor injection for dependencies
4. **Error Handling**: Services throw typed errors for consistent error handling
5. **Async by Default**: All service methods are async for better performance

## Usage Examples

### From SPFx Web Part
```typescript
import { ExternalUserService } from '../../../services/implementations/ExternalUserService';
import { GraphClientAdapter } from './GraphClientAdapter';

// Create adapter to bridge SPFx context to service
const graphAdapter = new GraphClientAdapter(this.context);

// Create service instance
const userService = new ExternalUserService(graphAdapter);

// Use the service
await userService.inviteUser(email, libraryUrl, permission);
```

### From Backend API
```typescript
import { ExternalUserService } from '../../services/implementations/ExternalUserService';
import { ServerGraphClient } from './ServerGraphClient';

// Create Graph client for server-side
const graphClient = new ServerGraphClient(credentials);

// Create service instance
const userService = new ExternalUserService(graphClient);

// Use the service
await userService.inviteUser(email, libraryUrl, permission);
```

## Migration Guide

### For SPFx Developers
1. Replace direct calls to `SharePointDataService` with shared services
2. Use adapters to convert SPFx context to service dependencies
3. Move business logic from components to services

### For API Developers
1. Replace inline Graph API calls with shared services
2. Use server-side implementations of service dependencies
3. Keep HTTP endpoint logic minimal, delegate to services
