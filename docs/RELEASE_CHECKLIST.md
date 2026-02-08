# Release Checklist

This checklist ensures all necessary steps are completed before deploying a new version of the SharePoint External User Manager to production.

## Overview

Use this checklist for every release to production. All items must be verified before deploying to ensure stability, security, and quality.

---

## Pre-Release Preparation

### Code Quality

- [ ] **All CI quality gates pass**
  - [ ] SPFx client builds and packages successfully
  - [ ] Azure Functions API builds without errors
  - [ ] .NET API builds and compiles successfully
  - [ ] Blazor Portal builds without errors
  - [ ] All linting checks pass
  - [ ] All automated tests pass

- [ ] **Code review completed**
  - [ ] All pull requests have been reviewed and approved
  - [ ] No open review comments remain unresolved
  - [ ] Code follows project conventions and style guide

- [ ] **Branch status**
  - [ ] `develop` branch is fully merged into `main`
  - [ ] No merge conflicts exist
  - [ ] Branch is up-to-date with main

### Security Verification

- [ ] **Security scans complete**
  - [ ] No secrets or credentials in repository
  - [ ] Secret scanning passed (TruffleHog)
  - [ ] Dependency vulnerability scan completed
  - [ ] No critical or high severity vulnerabilities
  - [ ] CodeQL security analysis passed

- [ ] **Secrets management verified**
  - [ ] All production secrets stored in Azure Key Vault
  - [ ] Local settings files are in `.gitignore`
  - [ ] No hardcoded connection strings or API keys
  - [ ] Environment variables properly configured in Azure

- [ ] **Tenant isolation verified**
  - [ ] All database queries include `TenantId` filter
  - [ ] Multi-tenant middleware is active on all endpoints
  - [ ] Tenant context is validated on every request
  - [ ] Cross-tenant data access is impossible

### Testing

- [ ] **Unit tests**
  - [ ] All unit tests pass locally
  - [ ] All unit tests pass in CI pipeline
  - [ ] Code coverage meets minimum threshold (if configured)

- [ ] **Integration tests**
  - [ ] API endpoints tested with Postman/Insomnia
  - [ ] SPFx web parts tested in SharePoint
  - [ ] Blazor portal tested in browser
  - [ ] Authentication flow tested end-to-end

- [ ] **Manual testing in dev environment**
  - [ ] Tenant onboarding flow works correctly
  - [ ] Client space creation provisions SharePoint sites
  - [ ] External user invitation and removal work
  - [ ] Library and list creation function properly
  - [ ] Stripe billing integration works (test mode)
  - [ ] Audit logs are created for all admin actions

### Database

- [ ] **Database migrations**
  - [ ] All EF Core migrations are up-to-date
  - [ ] Migrations tested in dev environment
  - [ ] Database backup created before migration
  - [ ] Rollback plan documented

- [ ] **Data integrity**
  - [ ] All tables include `TenantId` column
  - [ ] Indexes are created for performance
  - [ ] Foreign key constraints are valid
  - [ ] No data loss expected from migration

### Infrastructure

- [ ] **Azure resources verified**
  - [ ] All required Azure resources exist in production
  - [ ] Resource names follow naming conventions
  - [ ] Resource groups are properly tagged
  - [ ] Bicep templates are up-to-date

- [ ] **Configuration verified**
  - [ ] App Service configuration matches requirements
  - [ ] Connection strings are correct
  - [ ] Application Insights is configured
  - [ ] Custom domains and SSL certificates are valid

### Documentation

- [ ] **Documentation updated**
  - [ ] README.md reflects current features
  - [ ] CHANGELOG.md includes all changes
  - [ ] API documentation is up-to-date
  - [ ] User guides reflect new features
  - [ ] Deployment documentation is accurate

- [ ] **Release notes prepared**
  - [ ] New features listed
  - [ ] Bug fixes documented
  - [ ] Breaking changes highlighted
  - [ ] Migration instructions provided (if needed)

---

## Release Process

### Version Management

- [ ] **Version numbers updated**
  - [ ] SPFx `package-solution.json` version incremented
  - [ ] API `package.json` version updated
  - [ ] .NET project versions updated
  - [ ] Version follows semantic versioning (MAJOR.MINOR.PATCH)

- [ ] **Git tags created**
  - [ ] Tag created on main branch: `v{MAJOR}.{MINOR}.{PATCH}`
  - [ ] Tag pushed to remote: `git push origin v{MAJOR}.{MINOR}.{PATCH}`

### Build and Package

- [ ] **Build artifacts created**
  - [ ] SPFx `.sppkg` package created
  - [ ] Azure Functions deployment package ready
  - [ ] .NET API published
  - [ ] Blazor Portal published

- [ ] **Artifacts verified**
  - [ ] Package sizes are reasonable
  - [ ] All required files included
  - [ ] No debug symbols in production builds
  - [ ] Configuration is set to `Release` mode

### Deployment

- [ ] **Pre-deployment verification**
  - [ ] Deployment window scheduled
  - [ ] Stakeholders notified of deployment
  - [ ] Maintenance window communicated to customers
  - [ ] Rollback plan prepared and tested

- [ ] **Database deployment**
  - [ ] Database backup created
  - [ ] Migrations run successfully
  - [ ] Data integrity verified post-migration
  - [ ] Connection pools recycled if needed

- [ ] **Application deployment**
  - [ ] API deployed to Azure App Service
  - [ ] Blazor Portal deployed to Azure App Service
  - [ ] Azure Functions deployed
  - [ ] SPFx package uploaded to App Catalog
  - [ ] SPFx package published in App Catalog

- [ ] **Configuration deployment**
  - [ ] Application settings updated in Azure
  - [ ] Secrets rotated if needed
  - [ ] Environment variables verified
  - [ ] Feature flags configured (if applicable)

---

## Post-Deployment Verification

### Smoke Tests

- [ ] **Basic functionality verified**
  - [ ] API health endpoint responds: `/health`
  - [ ] Blazor Portal loads successfully
  - [ ] SPFx web parts load in SharePoint
  - [ ] Authentication works correctly

- [ ] **Critical paths tested**
  - [ ] User can sign in to Blazor Portal
  - [ ] Tenant onboarding flow completes
  - [ ] Client space can be created
  - [ ] External users can be invited
  - [ ] Libraries and lists can be created

### Monitoring

- [ ] **Monitoring configured**
  - [ ] Application Insights shows green status
  - [ ] No errors in application logs
  - [ ] Performance metrics are normal
  - [ ] Custom alerts are active

- [ ] **Log verification**
  - [ ] API logs show successful requests
  - [ ] Audit logs are being created
  - [ ] No unexpected errors in logs
  - [ ] Log retention policy is active

### Performance

- [ ] **Performance verified**
  - [ ] API response times are acceptable
  - [ ] Blazor Portal page load times are normal
  - [ ] SPFx web parts load quickly
  - [ ] Database queries are performant

### Security

- [ ] **Security posture verified**
  - [ ] HTTPS is enforced on all endpoints
  - [ ] Authentication is required
  - [ ] CORS settings are correct
  - [ ] Rate limiting is active

### Customer Communication

- [ ] **Release communicated**
  - [ ] Release notes published
  - [ ] Customers notified of new features
  - [ ] Known issues documented
  - [ ] Support team briefed on changes

---

## Post-Release Activities

### Monitoring Period

- [ ] **Monitor for 24 hours**
  - [ ] Check logs every 4 hours for first day
  - [ ] Monitor Application Insights for errors
  - [ ] Watch for customer-reported issues
  - [ ] Be prepared for emergency rollback

### Issue Tracking

- [ ] **Issue triage**
  - [ ] Create issues for any problems found
  - [ ] Prioritise critical bugs
  - [ ] Plan hotfix if needed
  - [ ] Update known issues list

### Documentation

- [ ] **Post-release documentation**
  - [ ] Update production environment documentation
  - [ ] Document any manual steps taken
  - [ ] Record lessons learned
  - [ ] Update troubleshooting guides

### Retrospective

- [ ] **Release retrospective**
  - [ ] What went well?
  - [ ] What could be improved?
  - [ ] Were there any surprises?
  - [ ] Action items for next release

---

## Emergency Rollback Procedure

If critical issues are discovered post-deployment:

### Immediate Actions

1. **Assess severity**
   - Determine if rollback is necessary
   - Check if issue affects all users or subset
   - Estimate time to fix vs rollback

2. **Initiate rollback** (if needed)
   - Notify stakeholders
   - Deploy previous version from artifacts
   - Restore database from backup (if needed)
   - Verify rollback successful

3. **Communication**
   - Notify customers of issue and resolution
   - Update status page
   - Post to internal channels

### Post-Rollback

4. **Root cause analysis**
   - Identify what went wrong
   - Document the issue
   - Create plan to prevent recurrence

5. **Fix and re-release**
   - Create hotfix branch
   - Implement fix
   - Run full checklist again
   - Deploy when ready

---

## Checklist Sign-Off

### Release Information

- **Version**: `v______._____._____`
- **Release Date**: `____________________`
- **Release Manager**: `____________________`

### Approvals

- [ ] **Development Lead**: __________________ (Date: ________)
- [ ] **QA Lead**: __________________ (Date: ________)
- [ ] **Security Lead**: __________________ (Date: ________)
- [ ] **Product Owner**: __________________ (Date: ________)

### Deployment Confirmation

- [ ] **Deployment completed successfully**: __________________ (Date: ________)
- [ ] **Post-deployment verification passed**: __________________ (Date: ________)
- [ ] **24-hour monitoring completed**: __________________ (Date: ________)

---

## Related Documentation

- [Branch Protection Rules](./BRANCH_PROTECTION.md) - Merge protection settings
- [Security Notes](./SECURITY_NOTES.md) - Security best practices
- [Deployment Guide](./DEPLOYMENT.md) - Detailed deployment instructions
- [Developer Guide](../DEVELOPER_GUIDE.md) - Local development setup

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-08 | Initial release checklist created |

---

**Maintained By**: Repository Administrators  
**Last Reviewed**: 2026-02-08
