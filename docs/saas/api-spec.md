# API Specification (OpenAPI 3.0)

## Overview

RESTful API for the SharePoint External User Manager SaaS backend. All endpoints require authentication via Azure AD Bearer tokens and enforce multi-tenant isolation.

**Base URL:** `https://api.spexternal.com/v1`

**Authentication:** OAuth 2.0 Bearer Token (Azure AD)

---

## OpenAPI Specification

```yaml
openapi: 3.0.3
info:
  title: SharePoint External User Manager API
  description: Multi-tenant SaaS API for managing external users and collaboration policies in SharePoint
  version: 1.0.0
  contact:
    name: API Support
    email: support@spexternal.com
    url: https://spexternal.com/support
  license:
    name: Proprietary
    url: https://spexternal.com/license

servers:
  - url: https://api.spexternal.com/v1
    description: Production
  - url: https://api-staging.spexternal.com/v1
    description: Staging
  - url: https://localhost:7071/api/v1
    description: Local Development

security:
  - BearerAuth: []

components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: Azure AD JWT token

  parameters:
    TenantId:
      name: X-Tenant-ID
      in: header
      required: true
      description: Tenant identifier from Entra ID
      schema:
        type: string
        format: uuid

  schemas:
    Error:
      type: object
      required:
        - success
        - error
      properties:
        success:
          type: boolean
          example: false
        error:
          type: object
          required:
            - code
            - message
          properties:
            code:
              type: string
              example: "UNAUTHORIZED"
            message:
              type: string
              example: "Invalid or missing authentication token"
            details:
              type: string
              example: "Token has expired"

    Library:
      type: object
      required:
        - id
        - name
        - siteUrl
        - externalUsersCount
      properties:
        id:
          type: string
          format: uuid
          example: "550e8400-e29b-41d4-a716-446655440000"
        sharePointSiteId:
          type: string
          example: "contoso.sharepoint.com,abc123,def456"
        sharePointLibraryId:
          type: string
          example: "lib-guid-789"
        name:
          type: string
          example: "External Projects"
          maxLength: 255
        description:
          type: string
          example: "Documents shared with external partners"
          maxLength: 1000
        siteUrl:
          type: string
          format: uri
          example: "https://contoso.sharepoint.com/sites/external-projects"
        owner:
          type: object
          properties:
            email:
              type: string
              format: email
              example: "john.doe@contoso.com"
            displayName:
              type: string
              example: "John Doe"
        externalUsersCount:
          type: integer
          minimum: 0
          example: 5
        permissions:
          type: string
          enum: [Read, Contribute, Edit, FullControl]
          example: "Contribute"
        externalSharingEnabled:
          type: boolean
          example: true
        isActive:
          type: boolean
          example: true
        createdDate:
          type: string
          format: date-time
          example: "2024-01-15T10:30:00Z"
        modifiedDate:
          type: string
          format: date-time
          example: "2024-01-20T15:45:00Z"
        lastSyncDate:
          type: string
          format: date-time
          example: "2024-01-21T08:00:00Z"

    ExternalUser:
      type: object
      required:
        - id
        - email
        - displayName
        - status
      properties:
        id:
          type: string
          format: uuid
          example: "660e8400-e29b-41d4-a716-446655440001"
        sharePointUserId:
          type: string
          example: "i:0#.f|membership|partner@external.com"
        userPrincipalName:
          type: string
          example: "partner_external.com#EXT#@contoso.onmicrosoft.com"
        email:
          type: string
          format: email
          example: "partner@external.com"
        displayName:
          type: string
          example: "Jane Partner"
        company:
          type: string
          example: "External Corp"
          maxLength: 255
        project:
          type: string
          example: "Project Alpha"
          maxLength: 255
        invitedBy:
          type: string
          format: email
          example: "john.doe@contoso.com"
        invitedDate:
          type: string
          format: date-time
          example: "2024-01-10T09:15:00Z"
        acceptedDate:
          type: string
          format: date-time
          nullable: true
          example: "2024-01-11T14:30:00Z"
        lastAccessDate:
          type: string
          format: date-time
          nullable: true
          example: "2024-01-20T16:45:00Z"
        status:
          type: string
          enum: [Invited, Active, Suspended, Removed]
          example: "Active"
        permissions:
          type: array
          items:
            type: object
            properties:
              libraryId:
                type: string
                format: uuid
              libraryName:
                type: string
              permissionLevel:
                type: string
                enum: [Read, Contribute, Edit, FullControl]
              expirationDate:
                type: string
                format: date-time
                nullable: true

    Policy:
      type: object
      required:
        - id
        - policyName
        - policyType
        - isEnabled
      properties:
        id:
          type: string
          format: uuid
          example: "770e8400-e29b-41d4-a716-446655440002"
        policyName:
          type: string
          example: "External Sharing Policy"
          maxLength: 255
        policyType:
          type: string
          enum: [ExternalSharingPolicy, AccessReviewPolicy, ExpirationPolicy, CompliancePolicy]
          example: "ExternalSharingPolicy"
        isEnabled:
          type: boolean
          example: true
        configuration:
          type: object
          description: Policy-specific configuration (JSON)
          example:
            allowAnonymousLinks: false
            defaultExpiration: 90
            requireApproval: true
        appliesTo:
          type: string
          enum: [AllLibraries, SpecificLibraries]
          example: "AllLibraries"
        libraries:
          type: array
          items:
            type: string
            format: uuid
          example: ["550e8400-e29b-41d4-a716-446655440000"]
        createdDate:
          type: string
          format: date-time
        createdBy:
          type: string
        modifiedDate:
          type: string
          format: date-time
        modifiedBy:
          type: string

    AuditEvent:
      type: object
      required:
        - id
        - eventType
        - timestamp
      properties:
        id:
          type: string
          format: uuid
        eventType:
          type: string
          example: "UserInvited"
        entityType:
          type: string
          enum: [Library, User, Permission, Policy]
          example: "User"
        entityId:
          type: string
          format: uuid
        action:
          type: string
          enum: [Create, Update, Delete, Grant, Revoke]
          example: "Create"
        actionBy:
          type: string
          example: "john.doe@contoso.com"
        timestamp:
          type: string
          format: date-time
          example: "2024-01-15T10:30:00Z"
        details:
          type: object
          description: Event-specific details (JSON)
        ipAddress:
          type: string
          example: "203.0.113.42"
        userAgent:
          type: string
          example: "Mozilla/5.0..."

    Subscription:
      type: object
      required:
        - tenantId
        - tier
        - status
      properties:
        tenantId:
          type: string
          format: uuid
        tier:
          type: string
          enum: [Free, Pro, Enterprise]
          example: "Pro"
        status:
          type: string
          enum: [Active, Trial, Expired, Suspended, Cancelled]
          example: "Active"
        startDate:
          type: string
          format: date-time
        endDate:
          type: string
          format: date-time
          nullable: true
        trialEndDate:
          type: string
          format: date-time
          nullable: true
        features:
          type: object
          properties:
            maxUsers:
              type: integer
            maxLibraries:
              type: integer
            bulkOperations:
              type: boolean
            advancedAudit:
              type: boolean
        usage:
          type: object
          properties:
            currentUsers:
              type: integer
            currentLibraries:
              type: integer
            apiCallsThisMonth:
              type: integer

    PaginationMeta:
      type: object
      properties:
        page:
          type: integer
          minimum: 1
          example: 1
        pageSize:
          type: integer
          minimum: 1
          maximum: 100
          example: 50
        total:
          type: integer
          example: 125
        hasNext:
          type: boolean
          example: true

paths:
  # Tenant Onboarding Endpoints
  /tenants/register:
    post:
      summary: Register new tenant
      description: Onboard a new tenant to the SaaS platform
      tags:
        - Tenant Onboarding
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - tenantDomain
                - displayName
                - adminEmail
              properties:
                tenantDomain:
                  type: string
                  example: "contoso.com"
                displayName:
                  type: string
                  example: "Contoso Corporation"
                adminEmail:
                  type: string
                  format: email
                  example: "admin@contoso.com"
      responses:
        '201':
          description: Tenant registered successfully
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    type: object
                    properties:
                      tenantId:
                        type: string
                        format: uuid
                      status:
                        type: string
                        example: "Provisioning"
                      nextSteps:
                        type: array
                        items:
                          type: string
                        example: ["Grant admin consent", "Configure settings"]
        '400':
          description: Bad request
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'
        '409':
          description: Tenant already exists
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  /tenants/verify:
    post:
      summary: Verify admin identity
      description: Verify that the user is a tenant administrator
      tags:
        - Tenant Onboarding
      responses:
        '200':
          description: Verification successful
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    type: object
                    properties:
                      isAdmin:
                        type: boolean
                        example: true
                      roles:
                        type: array
                        items:
                          type: string
                        example: ["Global Administrator"]
        '403':
          description: Not a tenant administrator
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  # Library Management Endpoints
  /libraries:
    get:
      summary: List libraries
      description: Get all libraries for the authenticated tenant
      tags:
        - Libraries
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: page
          in: query
          schema:
            type: integer
            minimum: 1
            default: 1
        - name: pageSize
          in: query
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 50
        - name: search
          in: query
          schema:
            type: string
          description: Search by library name or description
        - name: owner
          in: query
          schema:
            type: string
          description: Filter by library owner email
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/Library'
                  pagination:
                    $ref: '#/components/schemas/PaginationMeta'
        '401':
          description: Unauthorized
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'
        '403':
          description: Forbidden
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

    post:
      summary: Create library
      description: Create a new SharePoint library for external sharing
      tags:
        - Libraries
      parameters:
        - $ref: '#/components/parameters/TenantId'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - name
                - siteUrl
              properties:
                name:
                  type: string
                  example: "Partner Collaboration"
                description:
                  type: string
                  example: "Shared workspace for external partners"
                siteUrl:
                  type: string
                  format: uri
                  example: "https://contoso.sharepoint.com/sites/partners"
                enableExternalSharing:
                  type: boolean
                  default: true
      responses:
        '201':
          description: Library created
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    $ref: '#/components/schemas/Library'
        '402':
          description: Payment required (license limit reached)
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  /libraries/{libraryId}:
    get:
      summary: Get library details
      description: Get detailed information about a specific library
      tags:
        - Libraries
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: libraryId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    $ref: '#/components/schemas/Library'
        '404':
          description: Library not found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

    delete:
      summary: Delete library
      description: Remove a library from management
      tags:
        - Libraries
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: libraryId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Library deleted
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  message:
                    type: string
                    example: "Library successfully deleted"
        '404':
          description: Library not found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  # External User Management Endpoints
  /libraries/{libraryId}/users:
    get:
      summary: List external users
      description: Get external users for a specific library
      tags:
        - External Users
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: libraryId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: page
          in: query
          schema:
            type: integer
            minimum: 1
            default: 1
        - name: pageSize
          in: query
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 50
        - name: status
          in: query
          schema:
            type: string
            enum: [Invited, Active, Suspended, Removed]
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/ExternalUser'
                  pagination:
                    $ref: '#/components/schemas/PaginationMeta'

    post:
      summary: Invite external user
      description: Invite an external user to access a library
      tags:
        - External Users
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: libraryId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - email
                - permissions
              properties:
                email:
                  type: string
                  format: email
                  example: "partner@external.com"
                displayName:
                  type: string
                  example: "Jane Partner"
                company:
                  type: string
                  example: "External Corp"
                project:
                  type: string
                  example: "Project Alpha"
                permissions:
                  type: string
                  enum: [Read, Contribute, Edit, FullControl]
                  example: "Contribute"
                message:
                  type: string
                  example: "Welcome to our collaboration workspace"
                expirationDays:
                  type: integer
                  minimum: 1
                  maximum: 365
                  example: 90
      responses:
        '201':
          description: User invited
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    $ref: '#/components/schemas/ExternalUser'
        '402':
          description: Payment required (user limit reached)
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  /libraries/{libraryId}/users/{userId}:
    put:
      summary: Update user permissions
      description: Update permissions for an external user
      tags:
        - External Users
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: libraryId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: userId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                permissions:
                  type: string
                  enum: [Read, Contribute, Edit, FullControl]
                expirationDate:
                  type: string
                  format: date-time
      responses:
        '200':
          description: Permissions updated
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    $ref: '#/components/schemas/ExternalUser'

    delete:
      summary: Revoke user access
      description: Remove external user from library
      tags:
        - External Users
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: libraryId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: userId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Access revoked
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  message:
                    type: string
                    example: "User access revoked successfully"

  # Policy Management Endpoints
  /policies:
    get:
      summary: List policies
      description: Get all collaboration policies for the tenant
      tags:
        - Policies
      parameters:
        - $ref: '#/components/parameters/TenantId'
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/Policy'

    post:
      summary: Create policy
      description: Create a new collaboration policy
      tags:
        - Policies
      parameters:
        - $ref: '#/components/parameters/TenantId'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - policyName
                - policyType
                - configuration
              properties:
                policyName:
                  type: string
                policyType:
                  type: string
                  enum: [ExternalSharingPolicy, AccessReviewPolicy, ExpirationPolicy, CompliancePolicy]
                isEnabled:
                  type: boolean
                  default: true
                configuration:
                  type: object
                appliesTo:
                  type: string
                  enum: [AllLibraries, SpecificLibraries]
                  default: AllLibraries
                libraries:
                  type: array
                  items:
                    type: string
                    format: uuid
      responses:
        '201':
          description: Policy created
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    $ref: '#/components/schemas/Policy'
        '403':
          description: Feature not available in current tier
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  # Audit Log Endpoints
  /audit-logs:
    get:
      summary: Query audit logs
      description: Get audit logs for the tenant
      tags:
        - Audit
      parameters:
        - $ref: '#/components/parameters/TenantId'
        - name: startDate
          in: query
          required: true
          schema:
            type: string
            format: date-time
        - name: endDate
          in: query
          required: true
          schema:
            type: string
            format: date-time
        - name: eventType
          in: query
          schema:
            type: string
        - name: actionBy
          in: query
          schema:
            type: string
        - name: page
          in: query
          schema:
            type: integer
            minimum: 1
            default: 1
        - name: pageSize
          in: query
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 50
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/AuditEvent'
                  pagination:
                    $ref: '#/components/schemas/PaginationMeta'
        '403':
          description: Feature not available in current tier
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  # Licensing Endpoints
  /subscription:
    get:
      summary: Get subscription status
      description: Get current subscription details for the tenant
      tags:
        - Licensing
      parameters:
        - $ref: '#/components/parameters/TenantId'
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    $ref: '#/components/schemas/Subscription'

  /subscription/upgrade:
    post:
      summary: Upgrade subscription
      description: Upgrade to a higher tier
      tags:
        - Licensing
      parameters:
        - $ref: '#/components/parameters/TenantId'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - tier
              properties:
                tier:
                  type: string
                  enum: [Pro, Enterprise]
                paymentMethod:
                  type: object
                  properties:
                    type:
                      type: string
                      example: "CreditCard"
                    token:
                      type: string
      responses:
        '200':
          description: Upgrade successful
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                    example: true
                  data:
                    $ref: '#/components/schemas/Subscription'

tags:
  - name: Tenant Onboarding
    description: Tenant registration and verification
  - name: Libraries
    description: Library management operations
  - name: External Users
    description: External user management operations
  - name: Policies
    description: Collaboration policy management
  - name: Audit
    description: Audit log querying
  - name: Licensing
    description: Subscription and licensing operations
```

## Rate Limiting

All endpoints are rate-limited based on subscription tier:

| Tier | Requests per Minute | Burst Limit |
|------|---------------------|-------------|
| Free | 60 | 100 |
| Pro | 300 | 500 |
| Enterprise | 1000 | 2000 |

Rate limit headers included in responses:
- `X-RateLimit-Limit`: Total requests allowed
- `X-RateLimit-Remaining`: Remaining requests
- `X-RateLimit-Reset`: Timestamp when limit resets

## Error Codes Reference

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `UNAUTHORIZED` | 401 | Invalid or missing authentication token |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `NOT_FOUND` | 404 | Resource not found |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `PAYMENT_REQUIRED` | 402 | Subscription expired or limit reached |
| `CONFLICT` | 409 | Resource already exists |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `INTERNAL_ERROR` | 500 | Internal server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

## Changelog

### v1.0.0 (2024-01-15)
- Initial API release
- Tenant onboarding endpoints
- Library and user management
- Policy management
- Audit logging
- Licensing endpoints

## Support

- **API Documentation:** https://docs.spexternal.com/api
- **Status Page:** https://status.spexternal.com
- **Support Email:** support@spexternal.com
- **Developer Forum:** https://community.spexternal.com
