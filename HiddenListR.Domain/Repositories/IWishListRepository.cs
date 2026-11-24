using HiddenListR.Domain.Entities;

namespace HiddenListR.Domain.Repositories;

public interface IWishListRepository
{
    Task<List<WishListItem>> GetItemsAsync(int eventId, int ownerUserId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, List<WishListItemForViewer>>> GetItemsForOtherUsersAsync(int eventId, IEnumerable<int> otherUserIds, int viewerUserId, CancellationToken cancellationToken = default);
    Task<WishListItem> AddItemAsync(int eventId, int ownerUserId, string title, string? description, string? productUrl, CancellationToken cancellationToken = default);
    Task<bool> UpdateItemAsync(int eventId, int itemId, int ownerUserId, string title, string? description, string? productUrl, CancellationToken cancellationToken = default);
    Task<int?> ClaimItemAsync(int eventId, int itemId, int claimingUserId, CancellationToken cancellationToken = default);
    Task<int?> UnclaimItemAsync(int eventId, int itemId, int claimingUserId, CancellationToken cancellationToken = default);
}
