# Quick Reference: Swagger Security Configuration

## 🚀 TL;DR

**Production (Recommended):**
```json
{
  "Swagger": {
    "Enabled": false
  }
}
```
✅ Swagger is completely disabled and inaccessible.

---

## 📋 Configuration Cheat Sheet

### Option 1: Disable Swagger (Most Secure)
**Use Case:** Production deployments, public-facing APIs

```json
{
  "Swagger": {
    "Enabled": false
  }
}
```

**Result:** ❌ Swagger UI not accessible at `/swagger`

---

### Option 2: Enable with Authentication
**Use Case:** Internal tools, admin access required

```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": true,
    "AllowedRoles": []
  }
}
```

**Result:** 
- ✅ Swagger UI accessible at `/swagger`
- 🔒 Requires valid JWT token
- 👥 Any authenticated user can access

---

### Option 3: Enable with Role-Based Access
**Use Case:** Admin-only access, privileged operations

```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": true,
    "AllowedRoles": ["Admin", "TenantOwner"]
  }
}
```

**Result:**
- ✅ Swagger UI accessible at `/swagger`
- 🔒 Requires valid JWT token
- 👑 Only users with Admin or TenantOwner role can access

---

### Option 4: Development Mode
**Use Case:** Local development

```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": false
  }
}
```

**Result:**
- ✅ Swagger UI accessible at `/swagger`
- 🔓 No authentication required
- ⚠️ Only use in Development environment

---

## 🌍 Environment Behavior

| Environment | Default Behavior | Override Possible? |
|-------------|------------------|-------------------|
| **Development** | Always enabled, no auth | No (security during dev) |
| **Staging** | Respects config | Yes |
| **Production** | Disabled by default | Yes (with auth) |

---

## 🔑 How to Get JWT Token

### For Testing

1. Sign in to your application
2. Open browser DevTools (F12)
3. Go to Network tab
4. Make any API request
5. Find `Authorization` header
6. Copy the token (starts with `Bearer`)

### In Swagger UI

1. Click **Authorize** button (top right)
2. Enter: `Bearer YOUR_TOKEN_HERE`
3. Click **Authorize**
4. Click **Close**

---

## 🛡️ Security Levels

```
Level 0: Development
├─ Swagger: ✅ Enabled
├─ Auth: ❌ Not required
└─ Use: Local development only

Level 1: Disabled (Production Default)
├─ Swagger: ❌ Disabled
├─ Auth: N/A
└─ Use: Public production APIs

Level 2: Authenticated (Production Optional)
├─ Swagger: ✅ Enabled
├─ Auth: ✅ JWT required
└─ Use: Internal APIs, authenticated users

Level 3: Role-Based (Production Optional)
├─ Swagger: ✅ Enabled
├─ Auth: ✅ JWT required
├─ Roles: ✅ Admin/TenantOwner
└─ Use: Admin-only access
```

---

## 🔧 Environment Variables

Override settings via environment variables:

```bash
# Disable Swagger
export Swagger__Enabled=false

# Enable with authentication
export Swagger__Enabled=true
export Swagger__RequireAuthentication=true

# Set allowed roles
export Swagger__AllowedRoles__0=Admin
export Swagger__AllowedRoles__1=TenantOwner
```

---

## ⚠️ Common Errors

### 401 Unauthorized
**Cause:** No JWT token provided

**Solution:**
```bash
# Add Authorization header
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://api.example.com/swagger
```

### 403 Forbidden
**Cause:** User doesn't have required role

**Solution:**
1. Check `AllowedRoles` configuration
2. Verify user has the role in Azure AD
3. Ensure role is in JWT token claims

### 404 Not Found
**Cause:** Swagger is disabled

**Solution:**
- Set `Swagger:Enabled` to `true` in configuration
- Or remove the setting (defaults to true in non-prod)

---

## 📊 Decision Tree

```
Should Swagger be accessible?
│
├─ Development environment?
│  └─ YES → Enable without auth ✅
│
├─ Production environment?
│  ├─ Public API?
│  │  └─ YES → Disable Swagger ❌
│  │
│  └─ Internal/Admin API?
│     ├─ Authentication available?
│     │  └─ YES → Enable with auth ✅
│     │
│     └─ Role-based access needed?
│        └─ YES → Enable with RBAC ✅
│
└─ Staging/Testing?
   └─ Use config-based approach ⚙️
```

---

## 🎯 Recommendations by Use Case

### Public SaaS API
```json
{
  "Swagger": {
    "Enabled": false
  }
}
```

### Enterprise Internal API
```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": true,
    "AllowedRoles": []
  }
}
```

### Admin/Operations API
```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": true,
    "AllowedRoles": ["Admin", "GlobalAdmin", "APIAdmin"]
  }
}
```

### Development/Staging
```json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuthentication": false
  }
}
```

---

## 📚 Related Documentation

- [Full Swagger Security Guide](./SWAGGER_SECURITY_GUIDE.md)
- [Implementation Summary](./ISSUE_01_08_IMPLEMENTATION_SUMMARY.md)
- [Security Summary](./ISSUE_01_08_SECURITY_SUMMARY.md)

---

## 🆘 Quick Troubleshooting

| Problem | Quick Fix |
|---------|-----------|
| Can't access Swagger in dev | Check environment is "Development" |
| Can't access Swagger in prod | Expected - it's disabled by default |
| Getting 401 errors | Add JWT token to Authorization header |
| Getting 403 errors | Check user has required role |
| Config not working | Restart application after config changes |

---

**Last Updated:** February 20, 2026  
**Status:** ✅ Production Ready  
**Security Level:** 🛡️ High  
