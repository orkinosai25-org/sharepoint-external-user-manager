using Microsoft.AspNetCore.Authorization;

namespace SharePointExternalUserManager.Api.Authorization;

/// <summary>
/// Requirement for role-based authorization
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public RoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}
