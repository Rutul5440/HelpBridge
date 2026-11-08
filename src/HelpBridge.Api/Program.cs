using System.Text;
using HelpBridge.Api.Hubs;
using HelpBridge.Api.Middleware;
using HelpBridge.Application.Interfaces;
using HelpBridge.Domain.Identity;
using HelpBridge.Infrastructure.AI;
using HelpBridge.Infrastructure.Identity;
using HelpBridge.Infrastructure.Payments;
using HelpBridge.Infrastructure.Persistence;
using HelpBridge.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Database & Persistence Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=HelpBridgeDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// 2. ASP.NET Core Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "HelpBridge_Super_Secure_Enterprise_Secret_Key_At_Least_32_Characters_Long_2026_!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HelpBridge.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HelpBridge.Clients";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Dependency Injection for Core Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IBlobStorageService, LocalOrBlobStorageService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddSignalR();

builder.Services.AddScoped<IPaymentProvider>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var providerName = config["Payments:Provider"] ?? "Simulated";
    if (providerName.Equals("Razorpay", StringComparison.OrdinalIgnoreCase))
    {
        return new RazorpayPaymentProvider(sp.GetRequiredService<HttpClient>(), config);
    }
    return new SimulatedPaymentProvider();
});

// 5. CORS Configuration for Angular Single Page App
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. Controllers & Swagger OpenAPI Documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HelpBridge Financial Assistance API",
        Version = "v1",
        Description = "Enterprise Modular Monolith REST API for verified financial assistance, payment processing, and double-entry ledger tracking."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

var app = builder.Build();

// 7. Auto-Migration & Seed System Initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        logger.LogInformation("Applying database migrations / ensuring created...");
        await db.Database.EnsureCreatedAsync();

        // Seed Core Roles
        string[] roles = ["Recipient", "Donor", "VerificationOfficer", "FinanceManager", "SupportAgent", "SuperAdmin"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed default Admin / Verification Officer for testing
        var adminEmail = "admin@helpbridge.org";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                PhoneNumber = "+919876543210",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, "HelpBridge@Admin2026!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(adminUser, ["SuperAdmin", "VerificationOfficer", "FinanceManager"]);
                var profile = new UserProfile(adminUser.Id, "Priya", "Admin", "Bengaluru", "Karnataka")
                {
                    IdentityVerified = true
                };
                db.UserProfiles.Add(profile);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded default administrative account: {AdminEmail}", adminEmail);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration/seeding.");
    }
}

// 8. HTTP Pipeline Configuration
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HelpBridge API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FundingHub>("/hubs/funding");

app.Run();

public partial class Program { }

