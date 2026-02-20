using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Middleware;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Services.Search;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Database context with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=SharePointExternalUserManager;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Entra ID authentication (multi-tenant)
// Check if AzureAd configuration exists before setting up authentication
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var hasAzureAdConfig = !string.IsNullOrWhiteSpace(azureAdSection["ClientId"]) && 
                       !string.IsNullOrWhiteSpace(azureAdSection["TenantId"]);

if (hasAzureAdConfig)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAdSection)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
        .AddInMemoryTokenCaches();
}
else
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    logger.LogWarning("Azure AD configuration is incomplete. Authentication is disabled.");
    logger.LogWarning("Configure AzureAd:ClientId, AzureAd:TenantId, and AzureAd:ClientSecret to enable authentication.");
    
    // Add minimal authentication setup to prevent errors
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.SaveToken = true;
        });
}

// Register services
builder.Services.AddScoped<ISharePointService, SharePointService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IPlanEnforcementService, PlanEnforcementService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddSingleton<ISearchService, SearchService>(); // Search service with mock data
builder.Services.AddHttpClient(); // For OAuth service HTTP calls

// AI Assistant services
builder.Services.AddMemoryCache(); // For rate limiting
builder.Services.AddHttpClient<AiAssistantService>();
builder.Services.AddScoped<AiAssistantService>();
builder.Services.AddScoped<PromptTemplateService>();
builder.Services.AddSingleton<AiRateLimitService>();

// Azure OpenAI configuration
var azureOpenAIConfig = builder.Configuration.GetSection("AzureOpenAI");
builder.Services.Configure<AzureOpenAIConfiguration>(options =>
{
    options.Endpoint = azureOpenAIConfig["Endpoint"] ?? "";
    options.ApiKey = azureOpenAIConfig["ApiKey"] ?? "";
    options.DeploymentName = azureOpenAIConfig["DeploymentName"] ?? "gpt-4";
    options.ApiVersion = azureOpenAIConfig["ApiVersion"] ?? "2024-08-01-preview";
    options.Model = azureOpenAIConfig["Model"] ?? "gpt-4";
    
    // Enable demo mode if endpoint/key not configured
    options.UseDemoMode = string.IsNullOrWhiteSpace(options.Endpoint) || 
                         string.IsNullOrWhiteSpace(options.ApiKey) ||
                         options.Endpoint.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI with comprehensive documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "SharePoint External User Manager API",
        Description = "Multi-tenant SaaS API for managing SharePoint external users, client spaces, and document libraries",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@clientspace.com"
        },
        License = new OpenApiLicense
        {
            Name = "Proprietary",
        }
    });

    // Add JWT Bearer authentication to Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments for better documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Order actions by the relative path then by HTTP method
    options.OrderActionsBy(apiDesc => $"{apiDesc.RelativePath}_{apiDesc.HttpMethod}");
});

// Configure rate limiting per tenant
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Extract tenant ID from JWT claims
        var tenantId = context.User?.FindFirst("tid")?.Value ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: tenantId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0 // No queueing - reject immediately when limit exceeded
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Customize the response when rate limit is exceeded
    options.OnRejected = async (context, cancellationToken) =>
    {
        var tenantId = context.HttpContext.User?.FindFirst("tid")?.Value ?? "anonymous";
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        
        logger.LogWarning(
            "Rate limit exceeded for tenant {TenantId} on path {Path}",
            tenantId,
            context.HttpContext.Request.Path);

        context.HttpContext.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "RATE_LIMIT_EXCEEDED",
            message = "Too many requests. Please try again later.",
            retryAfter = 60
        };
        
        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };
});

var app = builder.Build();

// Global exception handling middleware (must be early in pipeline)
app.UseGlobalExceptionHandler();

// Swagger configuration with security
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled", true);
var swaggerRequireAuth = builder.Configuration.GetValue<bool>("Swagger:RequireAuthentication", false);
var swaggerAllowedRoles = builder.Configuration.GetSection("Swagger:AllowedRoles").Get<string[]>() ?? Array.Empty<string>();

// In Development: Swagger is always enabled without authentication
// In Production: Swagger is disabled by default, but can be enabled with authentication
var enableSwagger = app.Environment.IsDevelopment() || (swaggerEnabled && !app.Environment.IsProduction());

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (swaggerEnabled && app.Environment.IsProduction() && swaggerRequireAuth)
{
    // Production with authentication required - enable Swagger with auth middleware
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/swagger"),
        appBuilder =>
        {
            appBuilder.Use(async (context, next) =>
            {
                // Check if user is authenticated
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "UNAUTHORIZED",
                        message = "Authentication required to access Swagger documentation"
                    });
                    return;
                }

                // Check if role-based access is configured
                if (swaggerAllowedRoles.Length > 0)
                {
                    var hasRequiredRole = swaggerAllowedRoles.Any(role =>
                        context.User.IsInRole(role) ||
                        context.User.HasClaim("roles", role));

                    if (!hasRequiredRole)
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "FORBIDDEN",
                            message = "Insufficient permissions to access Swagger documentation"
                        });
                        return;
                    }
                }

                await next();
            });
        });

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });
}

// Rate limiting middleware (after exception handler, before authentication)
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
