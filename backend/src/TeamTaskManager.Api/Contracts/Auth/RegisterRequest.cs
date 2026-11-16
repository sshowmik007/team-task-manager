namespace TeamTaskManager.Api.Contracts.Auth;

// CONTRACT (DTO):
// Defines what the client MUST provide to register a new account.
// Security: Keeps internal database fields (Id, PasswordHash, SecurityStamp, Roles) hidden from client input.
public class RegisterRequest
{
    // 'required' ensures the model binder rejects requests if Email is missing
    public required string Email { get; set; }

    // Plaintext password sent over HTTPS; backend hashes this before storing in database
    public required string Password { get; set; }

    // Friendly display name for team collaboration
    public required string DisplayName { get; set; }
}
