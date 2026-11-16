using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

// =========================================================================================
// ASP.NET CORE APPLICATION ENTRY POINT & DEPENDENCY INJECTION (DI) CONTAINER SETUP
// =========================================================================================
var builder = WebApplication.CreateBuilder(args);

// Read JWT secret configuration from appsettings.json.
// Fail fast at startup if the secret key is missing.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is missing.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// 1. MVC / API CONTROLLERS:
// Registers controller services into DI (enables [ApiController] attribute & routing).
builder.Services.AddControllers();

// 2. DATABASE PERSISTENCE (EF Core & PostgreSQL):
// Configures AppDbContext with Npgsql provider using the "DefaultConnection" connection string.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. ASP.NET CORE IDENTITY CORE:
// AddIdentityCore registers ONLY essential identity services without forcing cookies or UI.
// Configures password complexity rules and unique email constraints.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // Password rules: minimum 6 chars with at least one digit and lowercase character
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    // Connects Identity to EF Core so users & roles are saved in PostgreSQL via AppDbContext
    .AddEntityFrameworkStores<AppDbContext>();

// 4. AUTHENTICATION (JWT BEARER SCHEME):
// Tells ASP.NET Core: "When a request arrives, check the Authorization header for Bearer <token>".
// Validates cryptographic signature, issuer, audience, and expiration lifetime.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,           // Rejects expired tokens automatically
            ValidateIssuerSigningKey = true,   // Ensures token was signed by OUR secret key

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 5. AUTHORIZATION SERVICE:
// Enables policy enforcement and [Authorize] attribute validation across controllers.
builder.Services.AddAuthorization();

// 6. SWAGGER / OPENAPI DOCUMENTATION WITH JWT BEARER SUPPORT:
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Adds the "Authorize 🔓" button in Swagger UI to test protected endpoints with a JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    // Applies Bearer authentication globally to Swagger endpoints
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
});

var app = builder.Build();

// =========================================================================================
// HTTP REQUEST PIPELINE (MIDDLEWARE ORDER MATTERS!)
// =========================================================================================

// Enable Swagger UI only in Development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP requests to secure HTTPS
app.UseHttpsRedirection();

// MIDDLEWARE EXECUTION SEQUENCE:
// 1. UseAuthentication: "Who are you?" (Inspects Bearer token -> sets HttpContext.User)
app.UseAuthentication();

// 2. UseAuthorization:  "What are you allowed to do?" (Evaluates [Authorize] rules on endpoints)
app.UseAuthorization();

// Map route attributes from Controllers (e.g. [Route("api/projects")]) to incoming HTTP URLs
app.MapControllers();

// Start Kestrel web server
app.Run();
