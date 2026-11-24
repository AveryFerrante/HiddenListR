using HiddenListR.Domain.Data;
using HiddenListR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HiddenListR.Domain.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, List<string>>> GetUserEventTitlesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.EventMembers
            .OrderBy(em => em.Event.Title)
            .GroupBy(em => em.UserId)
            .Select(g => new { UserId = g.Key, Titles = g.Select(em => em.Event.Title).ToList() })
            .ToDictionaryAsync(x => x.UserId, x => x.Titles, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalized = phoneNumber.Trim();
        return await dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalized, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
