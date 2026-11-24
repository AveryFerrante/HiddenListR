namespace HiddenListR.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<EventMember> EventMembers { get; set; } = [];
    public ICollection<WishList> WishLists { get; set; } = [];
    public ICollection<WishListItemPurchase> Purchases { get; set; } = [];
}
