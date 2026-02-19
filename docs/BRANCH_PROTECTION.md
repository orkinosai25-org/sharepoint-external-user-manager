# Branch Protection Rules

This document outlines the recommended branch protection rules for the SharePoint External User Manager repository to ensure code quality and prevent broken merges.

## Overview

Branch protection rules prevent direct commits to important branches and enforce quality gates through required status checks. This ensures all code changes go through proper review and automated validation before merging.

## Recommended Settings for `main` Branch

### Required Status Checks

Configure the following status checks as **required** before merging:

#### 1. CI Quality Gates Workflow
All jobs from the `CI Quality Gates` workflow must pass:
- ✅ **SPFx Client - Build & Lint**
- ✅ **Azure Functions API - Build, Lint & Test**
- ✅ **.NET API - Build & Test**
- ✅ **Blazor Portal - Build & Test**
- ✅ **Security Scan - Secrets & Dependencies**
- ✅ **Quality Gates Summary**

#### 2. Status Checks Settings
- ☑️ **Require branches to be up to date before merging**: Enabled
- ☑️ **Require status checks to pass before merging**: Enabled
- ☑️ **Do not allow bypassing the above settings**: Enabled (for all including administrators)

### Pull Request Requirements

#### Required Reviews
- **Number of required approvals**: 1 (minimum)
- **Dismiss stale pull request approvals when new commits are pushed**: Enabled
- **Require review from Code Owners**: Enabled (if CODEOWNERS file exists)
- **Require approval of the most recent reviewable push**: Enabled

#### Restrictions
- **Restrict who can dismiss pull request reviews**: Repository administrators only
- **Allow specified actors to bypass required pull requests**: Disabled (enforce for everyone)

### Additional Protection Rules

#### Require Pull Requests
- ☑️ **Require a pull request before merging**: Enabled
- ☑️ **Require approvals**: 1 minimum
- ☑️ **Dismiss stale pull request approvals when new commits are pushed**: Enabled

#### Restrict Pushes
- ☑️ **Restrict pushes that create matching branches**: Enabled
- **Who can push**: No one (all changes via PR only)

#### Force Push & Deletions
- ☑️ **Do not allow force pushes**: Enabled
- ☑️ **Do not allow deletions**: Enabled

#### Conversation Resolution
- ☑️ **Require conversation resolution before merging**: Enabled
- All review comments must be resolved before merge is allowed

#### Linear History
- ☑️ **Require linear history**: Enabled (optional but recommended)
- Prevents merge commits; only allow squash or rebase

## How to Configure in GitHub

### Step 1: Navigate to Branch Protection Settings

1. Go to your repository on GitHub
2. Click **Settings** → **Branches**
3. Under "Branch protection rules", click **Add rule** (or edit existing)
4. In "Branch name pattern", enter: `main`

### Step 2: Enable Required Status Checks

Under "Protect matching branches", enable the following:

1. ☑️ **Require a pull request before merging**
   - Set "Required approvals" to: **1**
   - ☑️ Enable "Dismiss stale pull request approvals when new commits are pushed"
   - ☑️ Enable "Require approval of the most recent reviewable push"

2. ☑️ **Require status checks to pass before merging**
   - ☑️ Enable "Require branches to be up to date before merging"
   - Click "Add checks" and add these required checks:
     - `SPFx Client - Build & Lint`
     - `Azure Functions API - Build, Lint & Test`
     - `.NET API - Build & Test`
     - `Blazor Portal - Build & Test`
     - `Security Scan - Secrets & Dependencies`
     - `Quality Gates Summary`

3. ☑️ **Require conversation resolution before merging**

4. ☑️ **Do not allow bypassing the above settings**
   - This applies rules to administrators as well

5. ☑️ **Restrict who can push to matching branches**
   - Leave empty (no one can push directly)

6. ☑️ **Do not allow force pushes**

7. ☑️ **Do not allow deletions**

### Step 3: Save Changes

Click **Create** (or **Save changes** if editing)

## What These Rules Prevent

### ❌ Prevented Actions

1. **Direct commits to main**: All changes must go through pull requests
2. **Merging with failing tests**: Cannot merge if any tests fail
3. **Merging with build failures**: Cannot merge if SPFx, API, or Portal fail to build
4. **Merging with lint errors**: Cannot merge if linting fails
5. **Force pushing**: Cannot rewrite main branch history
6. **Deleting main branch**: Cannot accidentally delete the main branch
7. **Unreviewed code**: Cannot merge without at least one approval
8. **Unresolved comments**: Cannot merge with open review comments
9. **Out-of-date branches**: Cannot merge if branch is behind main

### ✅ Required Actions

To merge a pull request, the following must be true:
1. ✅ All CI quality gate checks pass (build, lint, test)
2. ✅ At least one reviewer has approved
3. ✅ All review comments are resolved
4. ✅ Branch is up-to-date with main
5. ✅ No merge conflicts exist
6. ✅ Security scans complete successfully

## Additional Recommendations

### Code Owners File

Create a `.github/CODEOWNERS` file to automatically request reviews from specific people or teams:

```plaintext
# Global owners
* @orkinosai25

# SPFx Client
/src/client-spfx/ @frontend-team @orkinosai25

# Backend API
/src/api-dotnet/ @backend-team @orkinosai25

# Blazor Portal
/src/portal-blazor/ @frontend-team @backend-team @orkinosai25

# Infrastructure
/infra/ @devops-team @orkinosai25

# Documentation
/docs/ @orkinosai25

# GitHub Actions
/.github/workflows/ @devops-team @orkinosai25
```

### Automated Dependency Updates

Consider enabling **Dependabot** for automated dependency updates:

1. Go to **Settings** → **Security & analysis**
2. Enable **Dependabot alerts**
3. Enable **Dependabot security updates**
4. Create `.github/dependabot.yml`:

```yaml
version: 2
updates:
  # SPFx dependencies
  - package-ecosystem: "npm"
    directory: "/src/client-spfx"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5

  # Azure Functions dependencies
  - package-ecosystem: "npm"
    directory: "/src/api-dotnet"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5

  # .NET dependencies
  - package-ecosystem: "nuget"
    directory: "/src/api-dotnet/WebApi/SharePointExternalUserManager.Api"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5

  # Blazor Portal dependencies
  - package-ecosystem: "nuget"
    directory: "/src/portal-blazor/SharePointExternalUserManager.Portal"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5

  # GitHub Actions
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

### Security Scanning

Enable GitHub's built-in security features:

1. **Secret scanning**: Settings → Security & analysis → Enable Secret scanning
2. **Code scanning**: Settings → Security & analysis → Set up CodeQL analysis
3. **Dependency graph**: Settings → Security & analysis → Enable Dependency graph

## Monitoring and Maintenance

### Regular Reviews

- **Weekly**: Review any open pull requests and ensure they're not stale
- **Monthly**: Review branch protection rules to ensure they're still appropriate
- **Quarterly**: Review security scan results and address any findings

### Metrics to Track

- Average time from PR creation to merge
- Number of PRs blocked by failing checks
- Number of security vulnerabilities found and fixed
- Code review participation rate

### Troubleshooting

#### Status Check Not Showing

If a required status check isn't showing:
1. Ensure the workflow has run at least once on a PR
2. Check that the job name exactly matches what's configured
3. Verify the workflow triggers on `pull_request` events for `main` branch

#### Unable to Merge Despite Passing Checks

If all checks pass but merge is blocked:
1. Check if all review comments are resolved
2. Verify the branch is up-to-date with main
3. Ensure at least one approval exists
4. Check if merge conflicts exist

#### Administrator Override

While not recommended, administrators can bypass branch protection if absolutely necessary:
1. Use the **"Merge without waiting for requirements to be met"** option
2. Document the reason in the PR description
3. Create a follow-up issue to address the skipped checks

## Related Documentation

- [ISSUE_F_CI_CD_IMPLEMENTATION.md](../ISSUE_F_CI_CD_IMPLEMENTATION.md) - Complete CI/CD documentation
- [ISSUE_F_SECURITY_SUMMARY.md](../ISSUE_F_SECURITY_SUMMARY.md) - CI/CD security considerations
- [WORKFLOW_SECRET_SETUP.md](../WORKFLOW_SECRET_SETUP.md) - GitHub Actions secrets configuration
- [Release Checklist](./RELEASE_CHECKLIST.md) - Pre-release verification steps
- [Security Notes](./SECURITY_NOTES.md) - Security best practices
- [Developer Guide](../DEVELOPER_GUIDE.md) - Local development setup
- [CI/CD Workflows](../.github/workflows/README.md) - Automated pipeline documentation

## Support

For questions or issues with branch protection:
1. Review [GitHub's branch protection documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/defining-the-mergeability-of-pull-requests/about-protected-branches)
2. Check existing repository issues
3. Contact the repository maintainers

---

**Last Updated**: 2026-02-19  
**Maintained By**: Repository Administrators  
**Related Issue**: ISSUE F — CI/CD and Deployment
