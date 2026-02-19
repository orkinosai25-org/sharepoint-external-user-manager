# Branch Protection Rules Setup Guide

This guide explains how to configure branch protection rules for the SharePoint External User Manager repository to ensure code quality and prevent accidental deployments.

## Overview

Branch protection rules help maintain code quality by:
- Requiring pull requests before merging
- Requiring status checks (CI workflows) to pass
- Requiring code reviews
- Preventing force pushes and deletions
- Enforcing linear history

## Recommended Protection Rules

### Main Branch (Production)

The `main` branch should have the strictest protection since it deploys to production.

#### Configuration Steps

1. Go to your repository on GitHub
2. Navigate to **Settings** > **Branches**
3. Click **Add rule** (or edit existing rule for `main`)
4. Configure the following:

#### Branch Name Pattern
```
main
```

#### Protection Settings

**✅ Require a pull request before merging**
- ✅ Require approvals: **1** (minimum)
- ✅ Dismiss stale pull request approvals when new commits are pushed
- ✅ Require review from Code Owners (if CODEOWNERS file exists)
- ⬜ Restrict who can dismiss pull request reviews (optional)
- ⬜ Allow specified actors to bypass pull request requirements (for emergencies only)
- ✅ Require approval of the most recent reviewable push

**✅ Require status checks to pass before merging**
- ✅ Require branches to be up to date before merging
- **Required status checks** (select these from the list):
  - `SPFx Client - Build & Lint`
  - `Azure Functions API - Build, Lint & Test`
  - `.NET API - Build & Test`
  - `Blazor Portal - Build & Test`
  - `Quality Gates Summary`
  - `Security Scan - Secrets & Dependencies` (optional, for advisory)

**✅ Require conversation resolution before merging**
- Ensures all comments on the PR are addressed

**✅ Require signed commits** (recommended but optional)
- Enhances security by verifying commit authors

**⬜ Require linear history** (recommended)
- Prevents merge commits, requires rebase or squash
- Keeps history clean and readable

**✅ Require deployments to succeed before merging** (optional)
- Select `production` environment
- Only if you want to require manual deployment approval before merge

**⬜ Lock branch** (not recommended for main)
- Only enable during critical incidents

**✅ Do not allow bypassing the above settings**
- Ensures rules apply to everyone

**⬜ Restrict who can push to matching branches** (optional)
- Limit to specific teams or users
- Useful for stricter control

**✅ Allow force pushes** → **Specify who can force push**
- Never allow force pushes to main
- If needed, specify only senior maintainers

**⬜ Allow deletions**
- Never allow main branch deletion

#### Rules Applied To

- ✅ Include administrators
- This ensures even repository admins must follow the rules

### Develop Branch (Development)

The `develop` branch has slightly relaxed rules for faster iteration.

#### Configuration Steps

1. Add another branch protection rule
2. Branch name pattern: `develop`

#### Protection Settings

**✅ Require a pull request before merging**
- ⬜ Require approvals: **0** or **1** (for development speed)
- ✅ Dismiss stale pull request approvals when new commits are pushed

**✅ Require status checks to pass before merging**
- ⬜ Require branches to be up to date before merging (optional for develop)
- **Required status checks**:
  - `SPFx Client - Build & Lint`
  - `Azure Functions API - Build, Lint & Test`
  - `.NET API - Build & Test`
  - `Blazor Portal - Build & Test`

**✅ Require conversation resolution before merging**

**⬜ Require linear history** (optional for develop)

**⬜ Allow force pushes** → **Specify who can force push**
- Allow developers to force push to develop for quick fixes
- Or leave disabled for cleaner history

**⬜ Allow deletions**
- Not recommended

## Status Checks Explained

### Required Checks for Main Branch

| Status Check | Purpose | Can Fail? |
|--------------|---------|-----------|
| SPFx Client - Build & Lint | Validates SharePoint web part code | Yes |
| Azure Functions API - Build, Lint & Test | Validates TypeScript backend code | Yes |
| .NET API - Build & Test | Validates .NET Web API code | Yes |
| Blazor Portal - Build & Test | Validates Blazor portal code | Yes |
| Quality Gates Summary | Overall status aggregation | Yes |
| Security Scan | Advisory security scan | No (continue-on-error) |

### How to Add Status Checks

Status checks appear in the branch protection rules after workflows have run at least once:

1. Push a change to `main` or open a PR
2. Wait for workflows to run
3. Status check names will appear in the dropdown
4. Select the checks you want to require

If checks don't appear:
- Ensure workflows have run at least once
- Check workflow names match what's in the YAML files
- Refresh the branch protection settings page

## Environment Protection Rules

Environments add an additional layer of protection for deployments.

### Production Environment Setup

1. Go to **Settings** > **Environments**
2. Click **New environment** or select existing `production` environment
3. Configure:

#### Environment Protection Rules

**✅ Required reviewers**
- Add at least 2 reviewers
- Deployment will pause until approved
- Choose senior developers or DevOps team members

**✅ Wait timer**
- Set to **0** minutes (no wait) or **5-10** minutes for automated checks
- Gives time to verify staging before production

**✅ Deployment branches**
- Select **Selected branches**
- Add pattern: `main`
- Ensures only main branch can deploy to production

#### Environment Secrets

Configure production-specific secrets here:
- `AZURE_CREDENTIALS_PROD`
- `API_APP_NAME_PROD`
- `PORTAL_APP_NAME_PROD`
- etc.

### Development Environment Setup

1. Create `development` environment
2. Configure:

#### Environment Protection Rules

**⬜ Required reviewers** (optional)
- Can leave empty for automatic deployment
- Or add 1 reviewer for oversight

**⬜ Wait timer** (optional)

**✅ Deployment branches**
- Select **Selected branches**
- Add pattern: `develop`

## CODEOWNERS File (Optional)

Create a `.github/CODEOWNERS` file to automatically request reviews from specific people or teams:

```
# Default owners for everything
*       @team-leads @devops-team

# API code owners
/src/api-dotnet/**    @backend-developers

# Portal code owners  
/src/portal-blazor/** @frontend-developers

# SPFx code owners
/src/client-spfx/**   @sharepoint-developers

# Infrastructure code owners
/infra/**             @devops-team

# Workflow owners
/.github/workflows/** @devops-team

# Documentation owners
/docs/**              @tech-writers
*.md                  @tech-writers
```

## Enabling Branch Protection

### Step-by-Step Checklist

- [ ] Configure `main` branch protection rules
- [ ] Configure `develop` branch protection rules
- [ ] Set up `production` environment with required reviewers
- [ ] Set up `development` environment
- [ ] Configure production secrets in `production` environment
- [ ] Configure development secrets in `development` environment
- [ ] Create CODEOWNERS file (optional)
- [ ] Test protection rules with a test PR
- [ ] Document any exceptions or bypasses
- [ ] Train team on new workflow

## Testing Branch Protection

### Create a Test Pull Request

1. **Create a feature branch**:
   ```bash
   git checkout -b test/branch-protection
   echo "test" > test.txt
   git add test.txt
   git commit -m "Test branch protection"
   git push origin test/branch-protection
   ```

2. **Open a PR** to `main`
3. **Observe**:
   - PR shows required status checks
   - Merge button is disabled until checks pass
   - Review is required before merging
   - All conversations must be resolved

4. **Approve and Merge** (after checks pass)
5. **Clean up**:
   ```bash
   git checkout main
   git pull origin main
   git branch -d test/branch-protection
   git push origin --delete test/branch-protection
   ```

## Bypass Scenarios

### When to Bypass (Emergencies Only)

Branch protection can be bypassed in emergencies:

1. **Critical Production Bug**:
   - Security vulnerability needs immediate patch
   - Production is down and needs hotfix

2. **How to Bypass**:
   - Only repository administrators can bypass
   - Document the reason in PR or issue
   - Create follow-up PR with proper review
   - Post-mortem to prevent future bypasses

### Temporary Bypass

If you need to temporarily disable protection:

1. Go to branch protection rules
2. Uncheck "Include administrators"
3. Make emergency change
4. Re-enable immediately after

**Important**: Always document bypasses and follow up with proper process.

## Common Issues

### "Cannot merge - required status checks missing"

**Cause**: Workflows didn't run or failed

**Solution**:
1. Check Actions tab for workflow status
2. Fix any failing workflows
3. Push new commit to trigger checks
4. Wait for all checks to pass

### "Review required but no reviewers available"

**Cause**: CODEOWNERS configured but owners not available

**Solution**:
1. Request review from available team member
2. Temporarily remove CODEOWNERS requirement (admin only)
3. Add backup reviewers to CODEOWNERS

### "Status check not appearing in list"

**Cause**: Workflow hasn't run yet or wrong job name

**Solution**:
1. Run workflow at least once
2. Verify job name in workflow YAML matches
3. Refresh branch protection settings page
4. Check workflow is not skipped due to path filters

## Best Practices

### Do's ✅

- ✅ Require at least 1 reviewer for main
- ✅ Require all status checks to pass
- ✅ Use environment protection for production
- ✅ Regularly review and update rules
- ✅ Document any exceptions
- ✅ Train team on protected branch workflow
- ✅ Use conventional commits for clarity
- ✅ Keep PR sizes small for easier review

### Don'ts ❌

- ❌ Don't bypass without documentation
- ❌ Don't allow force pushes to main
- ❌ Don't allow branch deletion
- ❌ Don't skip required reviews
- ❌ Don't push directly to protected branches
- ❌ Don't approve your own PRs
- ❌ Don't merge failing PRs
- ❌ Don't disable protection rules permanently

## Monitoring and Compliance

### Review Protection Effectiveness

Regularly review:
- Number of bypasses (should be near zero)
- Average time to merge (should be reasonable)
- Number of failed PRs before merge (quality indicator)
- Review participation (everyone should be involved)

### Audit Branch Protection Changes

Check audit log for:
- Who made changes to branch protection rules
- When changes were made
- What was changed
- Why it was changed (from commit message or issue)

Access audit log:
1. Go to **Settings** > **Audit log**
2. Filter by "protected_branch"
3. Review changes

## Additional Resources

- [GitHub Branch Protection Documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/defining-the-mergeability-of-pull-requests/about-protected-branches)
- [GitHub Environments Documentation](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
- [CODEOWNERS Documentation](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)
- [GitHub Actions Status Checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks)

## Support

If you encounter issues with branch protection:

1. Check this guide first
2. Review GitHub documentation
3. Ask in team chat or #devops channel
4. Contact repository administrators
5. Create an issue in the repository

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-02-19 | Initial branch protection guide | Copilot |

---

**Next Steps**: 
1. Review this guide with your team
2. Implement branch protection rules
3. Test with a sample PR
4. Train team members on the workflow
