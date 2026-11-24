using HiddenListR.Domain.Entities;

namespace HiddenListR.Domain.Repositories;

public enum ItemClaimState
{
    None,
    ClaimedByOther,
    ClaimedByMe
}

public record WishListItemForViewer(WishListItem Item, ItemClaimState Claim);
