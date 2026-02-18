using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Services;
using System.Reflection;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
