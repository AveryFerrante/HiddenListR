namespace HiddenListR.Domain.Entities;

public class WishList
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int OwnerUserId { get; set; }
    public DateTime CreatedDateUtc { get; set; }
    public DateTime? UpdatedDateUtc { get; set; }

    public Event Event { get; set; } = null!;
    public User OwnerUser { get; set; } = null!;
    public ICollection<WishListItem> Items { get; set; } = [];
}
