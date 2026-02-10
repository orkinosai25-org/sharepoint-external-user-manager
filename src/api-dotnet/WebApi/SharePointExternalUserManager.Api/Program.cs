using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Services;

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
