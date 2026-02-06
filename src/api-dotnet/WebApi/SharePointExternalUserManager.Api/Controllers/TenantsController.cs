using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var tenantId = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var upn = User.FindFirst("upn")?.Value;

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant or user claims"));

        var tenantInfo = new
        {
            tenantId,
            userId,
            userPrincipalName = upn,
            isActive = true,
            subscriptionTier = "Free"
        };

        return Ok(ApiResponse<object>.SuccessResponse(tenantInfo));
    }
}
