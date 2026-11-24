using System.ComponentModel.DataAnnotations;
using HiddenListR.Domain.Entities;
using HiddenListR.Domain.Repositories;
using HiddenListR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HiddenListR.Pages.Events;

public class DetailsModel(IEventRepository eventRepository, IWishListRepository wishListRepository, RequestUserContext userContext) : PageModel
{
    private const string ClaimedView = "my-claims";

    public User? CurrentUser => userContext.User;
    public Event? Event { get; private set; }
    public string CurrentViewLabel { get; private set; } = "My List";
    public List<WishListItem> MyItems { get; private set; } = [];
    public List<UserWishList> OtherWishLists { get; private set; } = [];
    public List<ClaimedItemView> ClaimedItems { get; private set; } = [];
    public int ActiveViewUserId { get; private set; }
    public bool ShowingClaimedItems { get; private set; }
    public UserWishList? SelectedOtherList { get; private set; }
    public int TotalItems { get; private set; }
    public int ClaimedCount { get; private set; }
    public int NotClaimedCount => TotalItems - ClaimedCount;
    public bool CanShowClaimCounts { get; private set; }
    public bool CanAddItems { get; private set; }
    public bool CanClaimItems { get; private set; }
    public bool HideBackLink { get; private set; }
    public int AddItemOwnerUserId { get; private set; }
    public string AddItemOwnerName { get; private set; } = "your list";

    [BindProperty(Name = "active_view", SupportsGet = true)]
    public string? ActiveView { get; set; }

    public NewItemInput Input { get; set; } = new();
    public NewItemInput EditInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        if (CurrentUser is null) return Forbid();
        if (!await LoadAsync(id, cancellationToken)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAddItemAsync(int id, int? ownerUserId, [FromForm(Name = "Input")] NewItemInput input, CancellationToken cancellationToken)
    {
        if (CurrentUser is null) return StatusCode(403);
        if (await eventRepository.GetByIdAsync(id, cancellationToken) is null) return NotFound();

        var targetUserId = ownerUserId ?? CurrentUser.Id;
        if (targetUserId != CurrentUser.Id && !CurrentUser.IsAdmin()) return StatusCode(403);
        if (!await eventRepository.IsMemberAsync(id, targetUserId, cancellationToken)) return StatusCode(403);
        if (!CurrentUser.IsAdmin() && !await eventRepository.IsMemberAsync(id, CurrentUser.Id, cancellationToken)) return StatusCode(403);

        if (!ModelState.IsValid)
        {
            Input = input;
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        await wishListRepository.AddItemAsync(id, targetUserId, input.Title.Trim(),
            string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            string.IsNullOrWhiteSpace(input.ProductUrl) ? null : input.ProductUrl.Trim(),
            cancellationToken);

        return RedirectToPage(new { id, active_view = targetUserId });
    }

    public async Task<IActionResult> OnPostEditItemAsync(int id, int itemId, int ownerUserId, string? active_view, [FromForm(Name = "EditInput")] NewItemInput editInput, CancellationToken cancellationToken)
    {
        if (CurrentUser is null) return StatusCode(403);
        if (await eventRepository.GetByIdAsync(id, cancellationToken) is null) return NotFound();
        var isCurrentMember = await eventRepository.IsMemberAsync(id, CurrentUser.Id, cancellationToken);
        if (!isCurrentMember && !CurrentUser.IsAdmin()) return StatusCode(403);
        if (ownerUserId != CurrentUser.Id && !CurrentUser.IsAdmin()) return StatusCode(403);

        if (!ModelState.IsValid)
        {
            EditInput = editInput;
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        var updated = await wishListRepository.UpdateItemAsync(id, itemId, ownerUserId, editInput.Title.Trim(),
            string.IsNullOrWhiteSpace(editInput.Description) ? null : editInput.Description.Trim(),
            string.IsNullOrWhiteSpace(editInput.ProductUrl) ? null : editInput.ProductUrl.Trim(),
            cancellationToken);
        if (!updated) return NotFound();

        var view = active_view == "my-claims" ? "my-claims" : ownerUserId.ToString();
        return RedirectToPage(new { id, active_view = view });
    }

    public async Task<IActionResult> OnPostClaimItemAsync(int id, int itemId, CancellationToken cancellationToken)
    {
        if (CurrentUser is null) return Forbid();
        if (!await eventRepository.IsMemberAsync(id, CurrentUser.Id, cancellationToken)) return Forbid();
        var ownerUserId = await wishListRepository.ClaimItemAsync(id, itemId, CurrentUser.Id, cancellationToken);
        if (ownerUserId is null) return BadRequest();
        return RedirectToPage(new { id, active_view = ownerUserId });
    }

    public async Task<IActionResult> OnPostUnclaimItemAsync(int id, int itemId, string? active_view, CancellationToken cancellationToken)
    {
        if (CurrentUser is null) return Forbid();
        if (!await eventRepository.IsMemberAsync(id, CurrentUser.Id, cancellationToken)) return Forbid();
        var ownerUserId = await wishListRepository.UnclaimItemAsync(id, itemId, CurrentUser.Id, cancellationToken);
        if (ownerUserId is null) return BadRequest();
        var view = active_view == "my-claims" ? "my-claims" : ownerUserId.ToString();
        return RedirectToPage(new { id, active_view = view });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken cancellationToken)
    {
        Event = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (Event is null || CurrentUser is null) return false;
        var isCurrentMember = await eventRepository.IsMemberAsync(id, CurrentUser.Id, cancellationToken);
        if (!isCurrentMember && !CurrentUser.IsAdmin()) return false;
        CanClaimItems = isCurrentMember;
        HideBackLink = !CurrentUser.IsAdmin()
                       && (await eventRepository.GetForUserAsync(CurrentUser.Id, cancellationToken)).Count == 1;

        MyItems = await wishListRepository.GetItemsAsync(id, CurrentUser.Id, cancellationToken);

        var members = await eventRepository.GetMembersAsync(id, cancellationToken);
        var others = members.Where(m => m.Id != CurrentUser.Id).ToList();
        var itemsByUser = await wishListRepository.GetItemsForOtherUsersAsync(id, others.Select(m => m.Id), CurrentUser.Id, cancellationToken);
        OtherWishLists = others.Select(u => new UserWishList(u, itemsByUser.GetValueOrDefault(u.Id) ?? [])).ToList();
        ClaimedItems = OtherWishLists
            .SelectMany(wl => wl.Items
                .Where(i => i.Claim == ItemClaimState.ClaimedByMe)
                .Select(i => new ClaimedItemView(i.Item, $"{wl.User.FirstName} {wl.User.LastName}".Trim(), wl.User.Id)))
            .ToList();

        if (ActiveView == ClaimedView)
        {
            ShowingClaimedItems = true;
            CurrentViewLabel = "My Claimed Items";
            ActiveViewUserId = CurrentUser.Id;
        }
        else if (int.TryParse(ActiveView, out var requestedUserId)
                 && requestedUserId != CurrentUser.Id
                 && others.Any(m => m.Id == requestedUserId))
        {
            ActiveViewUserId = requestedUserId;
            SelectedOtherList = OtherWishLists.First(w => w.User.Id == requestedUserId);
            var owner = SelectedOtherList.User;
            var ownerInitial = owner.LastName.Length == 0 ? "" : owner.LastName[0].ToString();
            CurrentViewLabel = $"{owner.FirstName} {ownerInitial}'s List".Trim();
        }
        else
        {
            ActiveViewUserId = CurrentUser.Id;
        }

        AddItemOwnerUserId = SelectedOtherList?.User.Id ?? CurrentUser.Id;
        AddItemOwnerName = SelectedOtherList is null
            ? "your list"
            : $"{SelectedOtherList.User.FirstName} {SelectedOtherList.User.LastName}".Trim();
        CanAddItems = !ShowingClaimedItems
                      && (SelectedOtherList is null ? isCurrentMember : CurrentUser.IsAdmin());

        if (ShowingClaimedItems)
        {
            TotalItems = ClaimedItems.Count;
            CanShowClaimCounts = false;
        }
        else if (SelectedOtherList is not null)
        {
            TotalItems = SelectedOtherList.Items.Count;
            ClaimedCount = SelectedOtherList.Items.Count(i => i.Claim != ItemClaimState.None);
            CanShowClaimCounts = true;
        }
        else
        {
            TotalItems = MyItems.Count;
            CanShowClaimCounts = false;
        }
        return true;
    }

    public record UserWishList(User User, List<WishListItemForViewer> Items);

    public record ClaimedItemView(WishListItem Item, string CreatedBy, int OwnerUserId);

    public record WishListItemCardModel(WishListItem Item, int EventId, bool CanEdit, int OwnerUserId, bool CanClaim, bool CanUnclaim, ItemClaimState Claim, string? CreatedBy, string? ActiveView);

    public class NewItemInput
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        [StringLength(1000)]
        public string? Description { get; set; }
        [StringLength(500), Url]
        public string? ProductUrl { get; set; }
    }
}
