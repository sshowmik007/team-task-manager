namespace TeamTaskManager.Api.Contracts.Auth;

// CONTRACT (DTO):
// Defines credentials required for user authentication.
// Client posts Email + Password; backend verifies against PasswordHash and returns a signed JWT.
public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
