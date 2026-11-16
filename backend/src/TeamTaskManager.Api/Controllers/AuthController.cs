using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TeamTaskManager.Api.Contracts.Auth;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // UserManager<TUser> is an ASP.NET Core Identity service providing:
    // - Password hashing and salt verification (PBKDF2)
    // - User lookup by email / username
    // - Database persistence via EF Core
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    // HELPER: Generates a signed, cryptographically verified JSON Web Token (JWT)
    private string CreateToken(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");

        // CLAIMS: Key-value assertions about who the user is stored inside the token payload
        var claims = new List<Claim>
        {
            // NameIdentifier (sub) = The user's primary key (Guid)
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("displayName", user.DisplayName ?? string.Empty)
        };

        // Cryptographic symmetric key from server configuration
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        // Sign the token using HMAC-SHA256 algorithm
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // Assemble the JWT specification
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        // Serialize the token into the standard three-part base64 string: Header.Payload.Signature
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // POST: /api/auth/register
    // Creates a new user profile with a securely hashed password
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Map incoming Contract (DTO) to internal Model (Entity)
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        // CreateAsync automatically hashes the password using ASP.NET Core Identity's PBKDF2 hasher
        var result = await _userManager.CreateAsync(
            user,
            request.Password
        );

        // Return 400 Bad Request with validation errors if duplicate email or weak password
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Return safe user representation (never return PasswordHash!)
        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName
        });
    }

    // POST: /api/auth/login
    // Authenticates credentials and returns an access token
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // 1. Look up user by normalized email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Generic error message prevents username harvesting / account enumeration
            return Unauthorized("Invalid email or password.");
        }

        // 2. Verify plaintext password against the stored password hash + salt
        var passwordValid =
             await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            return Unauthorized("Invalid email or password.");
        }

        // 3. Issue JWT access token for subsequent authorized API requests
        var token = CreateToken(user);

        return Ok(new
        {
            accessToken = token
        });
    }

    // GET: /api/auth/me
    // Protected endpoint to verify JWT authentication works end-to-end
    // [Authorize] requires a valid Bearer token in the Authorization header.
    // Without token -> 401 Unauthorized
    // With token    -> 200 OK with extracted claims from HttpContext.User
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        // Extract claims that were decoded and verified by JwtBearerMiddleware
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue("displayName");

        return Ok(new
        {
            UserId = userId,
            Email = email,
            DisplayName = displayName
        });
    }
}
