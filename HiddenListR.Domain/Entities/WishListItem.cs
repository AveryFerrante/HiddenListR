namespace HiddenListR.Domain.Entities;

public class WishListItem
{
    public int Id { get; set; }
    public int WishListId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ProductUrl { get; set; }
    public DateTime CreatedDateUtc { get; set; }
    public DateTime? UpdatedDateUtc { get; set; }

    public WishList WishList { get; set; } = null!;
    public WishListItemPurchase? Purchase { get; set; }
}
