using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Functions.Functions.TenantOnboarding;

public class RegisterTenantFunction
{
    private readonly ILogger<RegisterTenantFunction> _logger;

    public RegisterTenantFunction(ILogger<RegisterTenantFunction> logger)
    {
        _logger = logger;
    }

    [Function("RegisterTenant")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/tenants/register")] HttpRequestData req)
    {
        _logger.LogInformation("Tenant registration request received");

        try
        {
            // Parse request body
            var requestBody = await req.ReadFromJsonAsync<TenantRegistrationRequest>();

            if (requestBody == null)
            {
                return await CreateErrorResponse(req, System.Net.HttpStatusCode.BadRequest, 
                    "VALIDATION_ERROR", "Invalid request body");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(requestBody.TenantDomain) ||
                string.IsNullOrWhiteSpace(requestBody.DisplayName) ||
                string.IsNullOrWhiteSpace(requestBody.AdminEmail))
            {
                return await CreateErrorResponse(req, System.Net.HttpStatusCode.BadRequest,
                    "VALIDATION_ERROR", "Required fields missing: tenantDomain, displayName, adminEmail");
            }

            // TODO: Implement actual tenant provisioning
            // 1. Check if tenant already exists
            // 2. Create tenant record in master database
            // 3. Provision tenant-specific database
            // 4. Initialize Cosmos DB containers
            // 5. Store connection strings in Key Vault
            // 6. Set up trial subscription

            var tenantId = Guid.NewGuid();

            _logger.LogInformation("Tenant registered successfully: {TenantId}", tenantId);

            // Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await response.WriteAsJsonAsync(ApiResponse<TenantRegistrationResponse>.SuccessResponse(
                new TenantRegistrationResponse
                {
                    TenantId = tenantId,
                    Status = "Provisioning",
                    NextSteps = new List<string>
                    {
                        "Grant admin consent",
                        "Configure initial settings",
                        "Install SPFx web part"
                    }
                }
            ));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering tenant");
            return await CreateErrorResponse(req, System.Net.HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", "An error occurred while registering the tenant");
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, 
        System.Net.HttpStatusCode statusCode, 
        string errorCode, 
        string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(errorCode, message));
        return response;
    }
}

public class TenantRegistrationRequest
{
    public string TenantDomain { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}

public class TenantRegistrationResponse
{
    public Guid TenantId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> NextSteps { get; set; } = new();
}
