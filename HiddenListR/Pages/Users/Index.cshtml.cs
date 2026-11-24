using System.ComponentModel.DataAnnotations;
using HiddenListR.Auth;
using HiddenListR.Domain.Entities;
using HiddenListR.Domain.Repositories;
using HiddenListR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HiddenListR.Pages.Users;

[AdminOnly]
public class IndexModel(IUserRepository userRepository, RequestUserContext userContext) : PageModel
{
    public User? CurrentUser => userContext.User;
    public List<User> Users { get; private set; } = [];
    public Dictionary<int, List<string>> UserEvents { get; private set; } = [];

    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadUsersAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {

        Input.PhoneNumber = Input.PhoneNumber?.Trim() ?? string.Empty;
        if (await userRepository.GetByPhoneNumberAsync(Input.PhoneNumber, cancellationToken) is not null)
            ModelState.AddModelError("Input.PhoneNumber", "A user with this phone number already exists.");

        if (!ModelState.IsValid)
        {
            await LoadUsersAsync(cancellationToken);
            return Page();
        }

        await userRepository.CreateAsync(new User
        {
            FirstName = Input.FirstName.Trim(),
            LastName = Input.LastName.Trim(),
            PhoneNumber = Input.PhoneNumber,
            Role = Input.Role
        }, cancellationToken);

        return RedirectToPage();
    }

    private async Task LoadUsersAsync(CancellationToken cancellationToken)
    {
        Users = await userRepository.GetAllAsync(cancellationToken);
        UserEvents = await userRepository.GetUserEventTitlesAsync(cancellationToken);
    }

    public class CreateUserInput
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;
    }
}
