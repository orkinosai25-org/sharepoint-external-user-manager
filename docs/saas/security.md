# Security Architecture

## Overview

The SharePoint External User Manager implements defense-in-depth security principles with multiple layers of protection. This document outlines the comprehensive security architecture covering authentication, authorization, data protection, network security, and compliance.

## Security Layers

```
┌─────────────────────────────────────────────────────────────────┐
│ Layer 1: Edge Protection                                        │
│ - Azure Front Door WAF (OWASP Top 10)                          │
│ - DDoS Protection Standard                                     │
│ - Rate Limiting & Throttling                                   │
│ - Geo-Filtering & IP Restrictions                              │
└─────────────────────────────────────────────────────────────────┘
                           │
┌─────────────────────────────────────────────────────────────────┐
│ Layer 2: Identity & Access Management                           │
│ - Microsoft Entra ID (Azure AD)                                │
│ - OAuth 2.0 / OpenID Connect                                   │
│ - Multi-Factor Authentication (MFA)                            │
│ - Conditional Access Policies                                  │
└─────────────────────────────────────────────────────────────────┘
                           │
┌─────────────────────────────────────────────────────────────────┐
│ Layer 3: Application Security                                   │
│ - JWT Token Validation                                         │
│ - Role-Based Access Control (RBAC)                             │
│ - Tenant Isolation Enforcement                                 │
│ - Input Validation & Sanitization                              │
└─────────────────────────────────────────────────────────────────┘
                           │
┌─────────────────────────────────────────────────────────────────┐
│ Layer 4: Data Security                                          │
│ - Encryption at Rest (TDE, Storage Encryption)                 │
│ - Encryption in Transit (TLS 1.2+)                             │
│ - Azure Key Vault for Secrets                                  │
│ - Database-per-Tenant Isolation                                │
└─────────────────────────────────────────────────────────────────┘
                           │
┌─────────────────────────────────────────────────────────────────┐
│ Layer 5: Monitoring & Response                                  │
│ - Application Insights (Security Events)                       │
│ - Azure Security Center                                        │
│ - Microsoft Sentinel (SIEM)                                    │
│ - Automated Threat Detection & Response                        │
└─────────────────────────────────────────────────────────────────┘
```

## Authentication & Authorization

### 1. Entra ID (Azure AD) Integration

**Multi-Tenant App Registration:**

```json
{
  "displayName": "SharePoint External User Manager",
  "signInAudience": "AzureADMultipleOrgs",
  "requiredResourceAccess": [
    {
      "resourceAppId": "00000003-0000-0000-c000-000000000000",
      "resourceAccess": [
        {
          "id": "df021288-bdef-4463-88db-98f22de89214",
          "type": "Role",
          "value": "User.Read.All"
        },
        {
          "id": "9492366f-7969-46a4-8d15-ed1a20078fff",
          "type": "Role",
          "value": "Sites.FullControl.All"
        }
      ]
    }
  ],
  "oauth2AllowImplicitFlow": false,
  "oauth2AllowIdTokenImplicitFlow": false,
  "oauth2Permissions": []
}
```

**Required Permissions:**
- `User.Read.All` - Read all users' profiles
- `Sites.ReadWrite.All` - Read and write SharePoint sites
- `Sites.FullControl.All` - Full control of SharePoint sites (admin operations)
- `Directory.Read.All` - Read directory data

### 2. OAuth 2.0 Authentication Flow

**Authorization Code Flow (Recommended for SPFx):**

```
1. Client → Azure AD: Authorization Request
   GET https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize
   ?client_id={client_id}
   &response_type=code
   &redirect_uri={redirect_uri}
   &scope=api://{api_app_id}/.default

2. Azure AD → Client: Authorization Code

3. Client → Azure AD: Token Request
   POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
   client_id={client_id}
   &code={authorization_code}
   &redirect_uri={redirect_uri}
   &grant_type=authorization_code
   &client_secret={client_secret}

4. Azure AD → Client: Access Token + Refresh Token

5. Client → API: API Request with Bearer Token
   Authorization: Bearer {access_token}
```

### 3. JWT Token Validation

**Backend Token Validation (C# Azure Functions):**

```csharp
public class AuthenticationMiddleware
{
    private readonly IConfiguration _configuration;
    
    public async Task<bool> ValidateTokenAsync(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
            
            ValidateAudience = true,
            ValidAudience = _configuration["EntraId:ClientId"],
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, identifier, parameters) =>
            {
                // Fetch signing keys from Azure AD
                var keys = await FetchAzureAdSigningKeysAsync();
                return keys;
            },
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
        
        var handler = new JwtSecurityTokenHandler();
        var claimsPrincipal = handler.ValidateToken(token, validationParameters, out _);
        
        return claimsPrincipal.Identity.IsAuthenticated;
    }
}
```

### 4. Role-Based Access Control (RBAC)

**Role Definitions:**

```csharp
public enum SystemRole
{
    SaasAdmin,      // Full system access, can manage all tenants
    TenantAdmin,    // Full access within tenant
    LibraryOwner,   // Manage specific libraries
    LibraryContributor, // Limited user management
    ReadOnly        // View-only access
}

public static class Permissions
{
    // Library permissions
    public const string LibrariesRead = "libraries:read";
    public const string LibrariesWrite = "libraries:write";
    public const string LibrariesDelete = "libraries:delete";
    
    // User permissions
    public const string UsersRead = "users:read";
    public const string UsersWrite = "users:write";
    public const string UsersDelete = "users:delete";
    
    // Policy permissions
    public const string PoliciesRead = "policies:read";
    public const string PoliciesWrite = "policies:write";
    
    // Settings permissions
    public const string SettingsRead = "settings:read";
    public const string SettingsWrite = "settings:write";
    
    // Audit permissions
    public const string AuditRead = "audit:read";
    public const string AuditExport = "audit:export";
}

public static Dictionary<SystemRole, List<string>> RolePermissions = new()
{
    {
        SystemRole.SaasAdmin,
        new List<string> { /* All permissions */ }
    },
    {
        SystemRole.TenantAdmin,
        new List<string>
        {
            Permissions.LibrariesRead,
            Permissions.LibrariesWrite,
            Permissions.LibrariesDelete,
            Permissions.UsersRead,
            Permissions.UsersWrite,
            Permissions.UsersDelete,
            Permissions.PoliciesRead,
            Permissions.PoliciesWrite,
            Permissions.SettingsRead,
            Permissions.SettingsWrite,
            Permissions.AuditRead,
            Permissions.AuditExport
        }
    },
    {
        SystemRole.LibraryOwner,
        new List<string>
        {
            Permissions.LibrariesRead,
            Permissions.LibrariesWrite,
            Permissions.UsersRead,
            Permissions.UsersWrite,
            Permissions.UsersDelete,
            Permissions.AuditRead
        }
    },
    {
        SystemRole.LibraryContributor,
        new List<string>
        {
            Permissions.LibrariesRead,
            Permissions.UsersRead,
            Permissions.UsersWrite
        }
    },
    {
        SystemRole.ReadOnly,
        new List<string>
        {
            Permissions.LibrariesRead,
            Permissions.UsersRead,
            Permissions.PoliciesRead
        }
    }
};
```

**Authorization Enforcement:**

```csharp
[FunctionName("GetLibraries")]
[Authorize(Permissions.LibrariesRead)]
public async Task<IActionResult> GetLibraries(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
{
    // Function protected by Authorize attribute
    // Middleware validates token and checks permission
}
```

## Tenant Isolation

### 1. Tenant Context Resolution

```csharp
public class TenantContextMiddleware
{
    public async Task<TenantContext> ResolveTenantAsync(HttpRequest request)
    {
        // Extract tenant ID from token claims
        var tenantIdClaim = request.HttpContext.User
            .FindFirst("http://schemas.microsoft.com/identity/claims/tenantid");
        
        if (tenantIdClaim == null)
            throw new UnauthorizedException("Tenant ID not found in token");
        
        var tenantId = tenantIdClaim.Value;
        
        // Validate tenant exists and is active
        var tenant = await _tenantService.GetTenantAsync(tenantId);
        
        if (tenant == null || !tenant.IsActive)
            throw new UnauthorizedException("Tenant not found or inactive");
        
        // Check subscription status
        if (tenant.SubscriptionStatus != SubscriptionStatus.Active)
            throw new PaymentRequiredException("Subscription expired or suspended");
        
        return new TenantContext
        {
            TenantId = tenant.TenantId,
            TenantDomain = tenant.TenantDomain,
            DatabaseName = tenant.DatabaseName,
            SubscriptionTier = tenant.SubscriptionTier
        };
    }
}
```

### 2. Database Connection Isolation

```csharp
public class TenantDatabaseService
{
    public async Task<SqlConnection> GetTenantConnectionAsync(Guid tenantId)
    {
        // Fetch tenant-specific connection string from Key Vault
        var connectionString = await _keyVaultService
            .GetSecretAsync($"sql-connection-tenant-{tenantId}");
        
        // Create isolated connection for tenant database
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        return connection;
    }
}
```

## Data Protection

### 1. Encryption at Rest

**Azure SQL Database:**
- **Transparent Data Encryption (TDE):** Enabled by default
- **Always Encrypted:** For sensitive columns (optional)
- **Column-level Encryption:** For PII data

**Azure Cosmos DB:**
- **Automatic Encryption:** All data encrypted at rest
- **Customer-Managed Keys:** Optional via Key Vault

**Azure Storage:**
- **Storage Service Encryption (SSE):** Enabled by default
- **Client-side Encryption:** For additional security

### 2. Encryption in Transit

**TLS Configuration:**
```json
{
  "minimumTlsVersion": "1.2",
  "cipherSuites": [
    "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384",
    "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256",
    "TLS_DHE_RSA_WITH_AES_256_GCM_SHA384",
    "TLS_DHE_RSA_WITH_AES_128_GCM_SHA256"
  ],
  "protocols": ["TLSv1.2", "TLSv1.3"]
}
```

### 3. Azure Key Vault Integration

**Secret Management:**

```csharp
public class KeyVaultService
{
    private readonly SecretClient _secretClient;
    
    public KeyVaultService(string keyVaultUrl)
    {
        var credential = new DefaultAzureCredential();
        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
    }
    
    public async Task<string> GetSecretAsync(string secretName)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, $"Failed to retrieve secret: {secretName}");
            throw;
        }
    }
    
    public async Task SetSecretAsync(string secretName, string secretValue)
    {
        await _secretClient.SetSecretAsync(secretName, secretValue);
    }
}
```

**Secrets Stored in Key Vault:**
- SQL connection strings (per tenant)
- Entra ID client secrets
- API keys for external services
- Encryption keys
- Certificate private keys

## Network Security

### 1. Azure Front Door & WAF

**WAF Rules:**
```json
{
  "wafPolicy": {
    "managedRules": {
      "managedRuleSets": [
        {
          "ruleSetType": "OWASP",
          "ruleSetVersion": "3.2"
        },
        {
          "ruleSetType": "Microsoft_BotManagerRuleSet",
          "ruleSetVersion": "1.0"
        }
      ]
    },
    "customRules": [
      {
        "name": "BlockSuspiciousIPs",
        "priority": 1,
        "ruleType": "MatchRule",
        "matchConditions": [
          {
            "matchVariable": "RemoteAddr",
            "operator": "IPMatch",
            "matchValue": ["1.2.3.4", "5.6.7.8"]
          }
        ],
        "action": "Block"
      },
      {
        "name": "RateLimitPerIP",
        "priority": 2,
        "ruleType": "RateLimitRule",
        "rateLimitThreshold": 100,
        "rateLimitDurationInMinutes": 1,
        "action": "Block"
      }
    ]
  }
}
```

### 2. Private Endpoints

**Azure SQL Private Endpoint:**
```bicep
resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2021-05-01' = {
  name: 'pe-sqlserver'
  location: location
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'sql-connection'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: ['sqlServer']
        }
      }
    ]
  }
}
```

### 3. Network Security Groups (NSG)

```bicep
resource nsg 'Microsoft.Network/networkSecurityGroups@2021-05-01' = {
  name: 'nsg-backend'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowHTTPS'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      },
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4096
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}
```

## Application Security

### 1. Input Validation

```csharp
public class InputValidator
{
    public static ValidationResult ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Error("Email is required");
        
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return ValidationResult.Error("Invalid email format");
        
        if (email.Length > 255)
            return ValidationResult.Error("Email too long");
        
        return ValidationResult.Success();
    }
    
    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        // Remove dangerous characters
        input = Regex.Replace(input, @"[<>""']", "");
        
        // Trim whitespace
        input = input.Trim();
        
        return input;
    }
}
```

### 2. SQL Injection Prevention

```csharp
// Always use parameterized queries
public async Task<ExternalUser> GetUserByEmailAsync(string email)
{
    using var connection = await GetConnectionAsync();
    
    var query = @"
        SELECT UserId, Email, DisplayName, Status 
        FROM ExternalUsers 
        WHERE Email = @Email AND IsActive = 1";
    
    var parameters = new { Email = email };
    
    return await connection.QueryFirstOrDefaultAsync<ExternalUser>(query, parameters);
}

// Never concatenate user input into SQL
// BAD: $"SELECT * FROM Users WHERE Email = '{email}'"  // VULNERABLE!
```

### 3. Cross-Site Scripting (XSS) Protection

**API Response Encoding:**
```csharp
public class ApiResponse<T>
{
    [JsonProperty("data")]
    public T Data { get; set; }
    
    // Automatically encode string properties
    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };
        
        return JsonConvert.SerializeObject(this, settings);
    }
}
```

### 4. Cross-Site Request Forgery (CSRF) Protection

```csharp
// For state-changing operations, validate origin
public class CsrfMiddleware
{
    public bool ValidateOrigin(HttpRequest request)
    {
        var origin = request.Headers["Origin"].FirstOrDefault();
        var referer = request.Headers["Referer"].FirstOrDefault();
        
        var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<List<string>>();
        
        if (!string.IsNullOrEmpty(origin))
            return allowedOrigins.Contains(origin);
        
        if (!string.IsNullOrEmpty(referer))
            return allowedOrigins.Any(o => referer.StartsWith(o));
        
        return false;
    }
}
```

## Monitoring & Threat Detection

### 1. Security Event Logging

```csharp
public class SecurityLogger
{
    private readonly ILogger _logger;
    
    public void LogAuthenticationSuccess(string userPrincipalName, string ipAddress)
    {
        _logger.LogInformation(
            "Authentication successful: User={User}, IP={IP}",
            userPrincipalName, ipAddress);
    }
    
    public void LogAuthenticationFailure(string reason, string ipAddress)
    {
        _logger.LogWarning(
            "Authentication failed: Reason={Reason}, IP={IP}",
            reason, ipAddress);
    }
    
    public void LogUnauthorizedAccess(string userPrincipalName, string resource)
    {
        _logger.LogWarning(
            "Unauthorized access attempt: User={User}, Resource={Resource}",
            userPrincipalName, resource);
    }
    
    public void LogSuspiciousActivity(string activity, string details)
    {
        _logger.LogError(
            "Suspicious activity detected: Activity={Activity}, Details={Details}",
            activity, details);
    }
}
```

### 2. Application Insights Security Alerts

```json
{
  "alerts": [
    {
      "name": "High Authentication Failure Rate",
      "condition": "Failed auth attempts > 10 in 5 minutes from same IP",
      "action": "Block IP, send alert"
    },
    {
      "name": "Unauthorized Access Attempts",
      "condition": "403 responses > 20 in 10 minutes",
      "action": "Send alert, review logs"
    },
    {
      "name": "Anomalous API Usage",
      "condition": "API calls spike > 500% from baseline",
      "action": "Send alert, check for abuse"
    },
    {
      "name": "Data Exfiltration Attempt",
      "condition": "Large data export requests",
      "action": "Block request, investigate"
    }
  ]
}
```

## Compliance & Auditing

### 1. Audit Trail

All security-relevant events are logged:
- Authentication attempts (success/failure)
- Authorization decisions
- Data access and modifications
- Permission grants and revocations
- Configuration changes
- Administrative actions

### 2. Compliance Standards

**SOC 2 Type II:**
- Access controls and authentication
- Encryption and data protection
- Monitoring and incident response
- Change management procedures

**GDPR:**
- Data minimization and purpose limitation
- Right to access and erasure
- Data portability
- Breach notification (72 hours)

**ISO 27001:**
- Information security management system
- Risk assessment and treatment
- Security policies and procedures
- Continuous improvement

### 3. Data Breach Response Plan

1. **Detection** (0-1 hour):
   - Automated alerts trigger
   - Security team notified
   - Initial assessment

2. **Containment** (1-4 hours):
   - Isolate affected systems
   - Revoke compromised credentials
   - Block malicious IPs

3. **Investigation** (4-24 hours):
   - Analyze logs and forensics
   - Determine scope and impact
   - Identify root cause

4. **Notification** (24-72 hours):
   - Notify affected customers
   - Regulatory reporting (if required)
   - Public disclosure (if necessary)

5. **Remediation** (1-2 weeks):
   - Fix vulnerabilities
   - Enhance security controls
   - Conduct post-mortem

## Security Best Practices

### Development
- Secure coding guidelines (OWASP)
- Code review for security issues
- Static application security testing (SAST)
- Dynamic application security testing (DAST)
- Dependency vulnerability scanning

### Operations
- Principle of least privilege
- Regular security patching
- Penetration testing (quarterly)
- Security awareness training
- Incident response drills

### Data
- Data classification and labeling
- Encryption for sensitive data
- Secure deletion procedures
- Data loss prevention (DLP)
- Regular backup testing

## References

- [Microsoft Security Best Practices](https://learn.microsoft.com/security/zero-trust/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Azure Security Baseline](https://learn.microsoft.com/security/benchmark/azure/)
- [Entra ID Security](https://learn.microsoft.com/entra/identity/)
