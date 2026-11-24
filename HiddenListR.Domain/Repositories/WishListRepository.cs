using HiddenListR.Domain.Data;
using HiddenListR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HiddenListR.Domain.Repositories;

public class WishListRepository(AppDbContext dbContext) : IWishListRepository
{
    public async Task<List<WishListItem>> GetItemsAsync(int eventId, int ownerUserId, CancellationToken cancellationToken = default)
    {
        var wishList = await dbContext.WishLists
            .AsNoTracking()
            .Include(wl => wl.Items)
            .FirstOrDefaultAsync(wl => wl.EventId == eventId && wl.OwnerUserId == ownerUserId, cancellationToken);
        return wishList?.Items.OrderByDescending(i => i.CreatedDateUtc).ToList() ?? [];
    }

    public async Task<Dictionary<int, List<WishListItemForViewer>>> GetItemsForOtherUsersAsync(int eventId, IEnumerable<int> otherUserIds, int viewerUserId, CancellationToken cancellationToken = default)
    {
        var ids = otherUserIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        var items = await dbContext.WishListItems
            .AsNoTracking()
            .Where(i => i.WishList.EventId == eventId && ids.Contains(i.WishList.OwnerUserId))
            .Select(i => new
            {
                i.WishList.OwnerUserId,
                Item = i,
                ClaimedByUserId = (int?)i.Purchase!.PurchasedByUserId
            })
            .ToListAsync(cancellationToken);
        return items
            .GroupBy(x => x.OwnerUserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new WishListItemForViewer(x.Item,
                        x.OwnerUserId == viewerUserId || x.ClaimedByUserId is null
                            ? ItemClaimState.None
                            : x.ClaimedByUserId == viewerUserId
                                ? ItemClaimState.ClaimedByMe
                                : ItemClaimState.ClaimedByOther))
                    .OrderByDescending(x => x.Item.CreatedDateUtc)
                    .ToList());
    }

    public async Task<WishListItem> AddItemAsync(int eventId, int ownerUserId, string title, string? description, string? productUrl, CancellationToken cancellationToken = default)
    {
        var wishList = await dbContext.WishLists
            .FirstOrDefaultAsync(wl => wl.EventId == eventId && wl.OwnerUserId == ownerUserId, cancellationToken);
        if (wishList is null)
        {
            wishList = new WishList
            {
                EventId = eventId,
                OwnerUserId = ownerUserId,
                CreatedDateUtc = DateTime.UtcNow
            };
            dbContext.WishLists.Add(wishList);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var item = new WishListItem
        {
            WishListId = wishList.Id,
            Title = title,
            Description = description,
            ProductUrl = productUrl,
            CreatedDateUtc = DateTime.UtcNow
        };
        dbContext.WishListItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> UpdateItemAsync(int eventId, int itemId, int ownerUserId, string title, string? description, string? productUrl, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.WishListItems
            .FirstOrDefaultAsync(i => i.Id == itemId
                                      && i.WishList.EventId == eventId
                                      && i.WishList.OwnerUserId == ownerUserId,
                cancellationToken);
        if (item is null) return false;

        item.Title = title;
        item.Description = description;
        item.ProductUrl = productUrl;
        item.UpdatedDateUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int?> ClaimItemAsync(int eventId, int itemId, int claimingUserId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.WishListItems
            .Where(i => i.Id == itemId && i.WishList.EventId == eventId && i.WishList.OwnerUserId != claimingUserId)
            .Select(i => new { i.WishList.OwnerUserId, Claimed = i.Purchase != null })
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null || item.Claimed) return null;

        dbContext.WishListItemPurchases.Add(new WishListItemPurchase
        {
            WishListItemId = itemId,
            PurchasedByUserId = claimingUserId,
            PurchasedDateUtc = DateTime.UtcNow
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return item.OwnerUserId;
        }
        catch (DbUpdateException)
        {
            return null;
        }
    }

    public async Task<int?> UnclaimItemAsync(int eventId, int itemId, int claimingUserId, CancellationToken cancellationToken = default)
    {
        var purchase = await dbContext.WishListItemPurchases
            .Include(p => p.WishListItem)
                .ThenInclude(i => i.WishList)
            .FirstOrDefaultAsync(p => p.WishListItemId == itemId
                                      && p.PurchasedByUserId == claimingUserId
                                      && p.WishListItem.WishList.EventId == eventId
                                      && p.WishListItem.WishList.OwnerUserId != claimingUserId,
                cancellationToken);
        if (purchase is null) return null;

        var ownerUserId = purchase.WishListItem.WishList.OwnerUserId;
        dbContext.WishListItemPurchases.Remove(purchase);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ownerUserId;
    }
}
