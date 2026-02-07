# ISSUE-09 Quick Reference Guide

## What Was Implemented

Converted SPFx client from direct Graph API calls to a thin SaaS client architecture.

## Core Files Created

### 1. SaaSApiClient (`shared/services/SaaSApiClient.ts`)

Shared API client for all webparts with:
- Tenant status checking (`/tenants/me`)
- Subscription status checking (`/billing/subscription/status`)
- AAD token authentication
- User-friendly error handling
- 5-minute caching

**Usage:**
```typescript
import { SaaSApiClient } from '../../../shared/services/SaaSApiClient';

const apiClient = new SaaSApiClient(context, backendUrl);

// Check tenant
const tenant = await apiClient.checkTenantStatus();
// { tenantId, isActive, subscriptionTier }

// Check subscription
const subscription = await apiClient.getSubscriptionStatus();
// { tier, status, isActive, limits, features }

// Make API request
const data = await apiClient.request('/endpoint', 'POST', body);
```

### 2. UI Components

**TenantConnectionStatus** (`shared/components/TenantConnectionStatus.tsx`)
- Shows onboarding status
- Displays errors
- Links to portal onboarding

**SubscriptionBanner** (`shared/components/SubscriptionBanner.tsx`)
- Shows trial expiry warnings
- Shows plan upgrade prompts
- Hidden for active paid plans

**UpgradeCallToAction** (`shared/components/UpgradeCallToAction.tsx`)
- Generic feature gating component
- Shows when features blocked by plan
- Links to pricing page

**Usage:**
```typescript
import { TenantConnectionStatus } from '../../../shared/components/TenantConnectionStatus';
import { SubscriptionBanner } from '../../../shared/components/SubscriptionBanner';

// In component render:
<TenantConnectionStatus 
  isConnected={tenantStatus?.isActive} 
  isLoading={checking}
  error={error}
  portalUrl="https://portal.yourcompany.com"
/>

<SubscriptionBanner 
  subscription={subscriptionStatus}
  portalUrl="https://portal.yourcompany.com"
/>
```

## Configuration

### WebPart Properties

Add to your WebPart interface:

```typescript
export interface IYourWebPartProps {
  backendApiUrl: string;  // Backend API URL
  portalUrl: string;       // Portal URL for upgrades
}
```

In PropertyPane:

```typescript
PropertyPaneTextField('backendApiUrl', {
  label: 'Backend API URL',
  description: 'URL of the SaaS backend API',
  placeholder: 'https://api.yourcompany.com/api'
}),
PropertyPaneTextField('portalUrl', {
  label: 'Portal URL',
  description: 'URL of the SaaS portal',
  placeholder: 'https://portal.yourcompany.com'
})
```

## Integration Pattern

### 1. Initialize in Component

```typescript
const [saasApiClient] = useState(() => 
  new SaaSApiClient(props.context, props.backendApiUrl)
);
const [tenantStatus, setTenantStatus] = useState<ITenantStatus | null>(null);
const [subscriptionStatus, setSubscriptionStatus] = useState<ISubscriptionStatus | null>(null);
```

### 2. Check Status on Mount

```typescript
useEffect(() => {
  const checkStatus = async () => {
    try {
      const tenant = await saasApiClient.checkTenantStatus();
      setTenantStatus(tenant);
      
      if (tenant.isActive) {
        const subscription = await saasApiClient.getSubscriptionStatus();
        setSubscriptionStatus(subscription);
      }
    } catch (error) {
      console.error('Status check failed:', error);
    }
  };
  
  checkStatus();
}, [saasApiClient]);
```

### 3. Show Status Banners

```typescript
return (
  <div>
    <TenantConnectionStatus 
      isConnected={tenantStatus?.isActive || false}
      isLoading={!tenantStatus}
      portalUrl={props.portalUrl}
    />
    
    {subscriptionStatus && (
      <SubscriptionBanner 
        subscription={subscriptionStatus}
        portalUrl={props.portalUrl}
      />
    )}
    
    {/* Your component content */}
  </div>
);
```

### 4. Feature Gating Example

```typescript
// Check if feature available
const canAddUsers = subscriptionStatus && 
  subscriptionStatus.limits.maxExternalUsersPerClient > currentUserCount;

if (!canAddUsers) {
  return (
    <UpgradeCallToAction 
      tier={subscriptionStatus.tier}
      feature="additional external users"
      portalUrl={props.portalUrl}
    />
  );
}
```

## Error Messages

Common errors translated automatically:

| Error Type | User Message |
|------------|--------------|
| Network failure | "Unable to connect to the service..." |
| 401 Unauthorized | "Authentication failed. Please sign in again." |
| 403 Forbidden | "Access denied..." |
| 404 Not Found | "Resource not found..." |
| 429 Rate Limit | "Too many requests. Please wait..." |
| 500+ Server Error | "Service temporarily unavailable..." |

## API Endpoints

### Tenant Status
```
GET /tenants/me
Returns: { tenantId, isActive, subscriptionTier }
```

### Subscription Status
```
GET /billing/subscription/status
Returns: { 
  tier, status, isActive, 
  startDate, endDate, trialExpiry,
  limits: { maxClients, maxExternalUsersPerClient, ... },
  features: [...]
}
```

### External Users (Example)
```
GET /external-users?library={url}
POST /external-users { email, library, permissions, ... }
DELETE /external-users { email, library }
```

## Build & Deploy

### TypeScript Compilation
```bash
cd src/client-spfx
nvm use 18
npm install
npm run build
```

### Configuration Files Updated
- `tsconfig.json` - Include webparts & shared folders
- `config/config.json` - Fixed manifest paths

### Known Issue
Webpack bundle build needs path config fix (TypeScript compiles fine).

## Testing Checklist

- [ ] Backend API URL configured
- [ ] Portal URL configured
- [ ] Tenant status check works
- [ ] Subscription status check works
- [ ] Status banners display
- [ ] Upgrade CTAs show when needed
- [ ] External user operations use API
- [ ] Errors are user-friendly
- [ ] No Graph API calls from SPFx

## Next Steps for Other Webparts

To refactor another webpart:

1. Add SaaSApiClient to component state
2. Add tenant/subscription checks on mount
3. Add status banners to UI
4. Update data services to use apiClient.request()
5. Add portalUrl property
6. Test with backend API

## Support

See `ISSUE_09_IMPLEMENTATION_SUMMARY.md` for:
- Detailed architecture
- Full API documentation
- Deployment guide
- Security analysis
- Troubleshooting

---

**Quick Start:** Copy pattern from ExternalUserManager component.  
**Documentation:** ISSUE_09_IMPLEMENTATION_SUMMARY.md  
**Questions:** Check implementation summary first
