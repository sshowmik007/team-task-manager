using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts.Auth;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    //UserManager: UserManager<ApplicationUser> is an ASP.NET Core Identity service.
    public AuthController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }


[HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password
        );

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName
        });
    }
}