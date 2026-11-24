using HiddenListR.Domain.Entities;

namespace HiddenListR.Domain.Repositories;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Event>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(int eventId, int userId, CancellationToken cancellationToken = default);
    Task<Event> CreateAsync(Event eventEntity, CancellationToken cancellationToken = default);
    Task<List<User>> GetMembersAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetMemberWishListItemCountsAsync(int eventId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(int eventId, int userId, CancellationToken cancellationToken = default);
    Task RemoveMembersAsync(int eventId, List<int> userIds, CancellationToken cancellationToken = default);
}
