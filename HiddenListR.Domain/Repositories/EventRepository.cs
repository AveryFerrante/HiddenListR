using HiddenListR.Domain.Data;
using HiddenListR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HiddenListR.Domain.Repositories;

public class EventRepository(AppDbContext dbContext) : IEventRepository
{
    public async Task<List<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Events
            .OrderByDescending(e => e.CreatedDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Event>> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Events
            .Where(e => e.EventMembers.Any(em => em.UserId == userId))
            .OrderByDescending(e => e.CreatedDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> IsMemberAsync(int eventId, int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EventMembers.AnyAsync(em => em.EventId == eventId && em.UserId == userId, cancellationToken);
    }

    public async Task<Event> CreateAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {
        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return eventEntity;
    }

    public async Task<List<User>> GetMembersAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EventMembers
            .Where(em => em.EventId == eventId)
            .Select(em => em.User)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetMemberWishListItemCountsAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WishLists
            .Where(wl => wl.EventId == eventId)
            .Select(wl => new { wl.OwnerUserId, wl.Items.Count })
            .ToDictionaryAsync(x => x.OwnerUserId, x => x.Count, cancellationToken);
    }

    public async Task AddMemberAsync(int eventId, int userId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.EventMembers.AnyAsync(em => em.EventId == eventId && em.UserId == userId, cancellationToken);
        if (exists) return;
        dbContext.EventMembers.Add(new EventMember { EventId = eventId, UserId = userId });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMembersAsync(int eventId, List<int> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return;
        var members = await dbContext.EventMembers
            .Where(em => em.EventId == eventId && userIds.Contains(em.UserId))
            .ToListAsync(cancellationToken);
        var wishLists = await dbContext.WishLists
            .Where(wl => wl.EventId == eventId && userIds.Contains(wl.OwnerUserId))
            .ToListAsync(cancellationToken);

        dbContext.WishLists.RemoveRange(wishLists);
        dbContext.EventMembers.RemoveRange(members);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
