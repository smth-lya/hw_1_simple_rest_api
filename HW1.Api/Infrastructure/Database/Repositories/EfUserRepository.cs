using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Models;
using HW1.Api.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HW1.Api.Infrastructure.Database.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public EfUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken: cancellationToken);
    }

    public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken: cancellationToken);
    }

   
    public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PagedResult<User>> GetUsersPagedAsync(int pageNumber, int pageSize)
    {
        return await GetUsersPagedAsync(new PaginationRequest() { PageNumber = pageNumber, PageSize = pageSize });
    }
    
    public async Task<PagedResult<User>> GetUsersPagedAsync(PaginationRequest request)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.Username)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<IEnumerable<User>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(u => u.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(u => u.CreatedAt <= toDate.Value);

        return await query.OrderBy(u => u.CreatedAt).ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task ClearAllAsync()
    {
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
    }
}