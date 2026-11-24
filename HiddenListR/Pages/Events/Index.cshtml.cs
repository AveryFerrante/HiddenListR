using System.ComponentModel.DataAnnotations;
using HiddenListR.Domain.Entities;
using HiddenListR.Domain.Repositories;
using HiddenListR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HiddenListR.Pages.Events;

public class IndexModel(IEventRepository eventRepository, IUserRepository userRepository, RequestUserContext userContext) : PageModel
{
    public User? CurrentUser => userContext.User;
    public List<Event> Events { get; private set; } = [];
    public Dictionary<int, List<User>> EventMembers { get; private set; } = [];
    public List<User> AllUsers { get; private set; } = [];

    [BindProperty]
    public CreateEventInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadEventsAsync(cancellationToken);
        if (CurrentUser is not null && !CurrentUser.IsAdmin() && Events.Count == 1)
            return RedirectToPage("/Events/Details", new { id = Events[0].Id });
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!CurrentUser.IsAdmin())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadEventsAsync(cancellationToken);
            return Page();
        }

        var eventEntity = new Event
        {
            Title = Input.Title.Trim(),
            CreatedDateUtc = DateTime.UtcNow,
            ExpireDateUtc = Input.ExpireDateUtc.HasValue
                ? DateTime.SpecifyKind(Input.ExpireDateUtc.Value, DateTimeKind.Utc)
                : null
        };

        await eventRepository.CreateAsync(eventEntity, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetMembersAsync(int eventId, CancellationToken cancellationToken)
    {
        if (!CurrentUser.IsAdmin()) return Forbid();
        var members = await eventRepository.GetMembersAsync(eventId, cancellationToken);
        var counts = await eventRepository.GetMemberWishListItemCountsAsync(eventId, cancellationToken);
        var allUsers = await userRepository.GetAllAsync(cancellationToken);
        var memberIds = members.Select(m => m.Id).ToHashSet();
        return new JsonResult(new
        {
            members = members.Select(m => new { m.Id, name = $"{m.FirstName} {m.LastName}", phone = m.PhoneNumber, wishListItemCount = counts.GetValueOrDefault(m.Id) }),
            availableUsers = allUsers.Where(u => !memberIds.Contains(u.Id)).Select(u => new { u.Id, name = $"{u.FirstName} {u.LastName}", phone = u.PhoneNumber })
        });
    }

    public async Task<IActionResult> OnPostAddMemberAsync([FromBody] MemberChangeRequest req, CancellationToken cancellationToken)
    {
        if (!CurrentUser.IsAdmin()) return Forbid();
        await eventRepository.AddMemberAsync(req.EventId, req.UserId, cancellationToken);
        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostRemoveMembersAsync([FromBody] RemoveMembersRequest req, CancellationToken cancellationToken)
    {
        if (!CurrentUser.IsAdmin()) return Forbid();
        await eventRepository.RemoveMembersAsync(req.EventId, req.UserIds ?? [], cancellationToken);
        return new JsonResult(new { ok = true });
    }

    public bool IsActive(Event ev) => ev.ExpireDateUtc is null || ev.ExpireDateUtc.Value > DateTime.UtcNow;

    private async Task LoadEventsAsync(CancellationToken cancellationToken)
    {
        if (CurrentUser is null)
        {
            Events = [];
            return;
        }

        Events = CurrentUser.IsAdmin()
            ? await eventRepository.GetAllAsync(cancellationToken)
            : await eventRepository.GetForUserAsync(CurrentUser.Id, cancellationToken);

        foreach (var ev in Events)
            EventMembers[ev.Id] = await eventRepository.GetMembersAsync(ev.Id, cancellationToken);

        if (CurrentUser.IsAdmin())
        {
            AllUsers = await userRepository.GetAllAsync(cancellationToken);
        }
    }

    public class MemberChangeRequest { public int EventId { get; set; } public int UserId { get; set; } }
    public class RemoveMembersRequest { public int EventId { get; set; } public List<int>? UserIds { get; set; } }

    public class CreateEventInput
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime? ExpireDateUtc { get; set; }
    }
}
