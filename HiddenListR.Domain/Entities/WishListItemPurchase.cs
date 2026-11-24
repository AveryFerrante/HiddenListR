namespace HiddenListR.Domain.Entities;

public class WishListItemPurchase
{
    public int Id { get; set; }
    public int WishListItemId { get; set; }
    public int PurchasedByUserId { get; set; }
    public DateTime PurchasedDateUtc { get; set; }

    public WishListItem WishListItem { get; set; } = null!;
    public User PurchasedByUser { get; set; } = null!;
}
