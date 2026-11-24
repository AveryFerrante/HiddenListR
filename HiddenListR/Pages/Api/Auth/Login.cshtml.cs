using System.Security.Claims;
using HiddenListR.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HiddenListR.Pages.Api.Auth;

[IgnoreAntiforgeryToken]
public class LoginModel(IUserRepository userRepository) : PageModel
{
    public IActionResult OnGet() => NotFound();

    public async Task<IActionResult> OnPostAsync([FromBody] LoginRequest? request, CancellationToken cancellationToken)
    {
        var phoneNumber = request?.PhoneNumber?.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest(new { message = "Phone number is required." });
        }

        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24),
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
        return new JsonResult(new { userId = user.Id });
    }

    public class LoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
