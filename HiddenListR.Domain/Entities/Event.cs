namespace HiddenListR.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime CreatedDateUtc { get; set; }
    public DateTime? ExpireDateUtc { get; set; }

    public ICollection<EventMember> EventMembers { get; set; } = [];
    public ICollection<WishList> WishLists { get; set; } = [];
}
