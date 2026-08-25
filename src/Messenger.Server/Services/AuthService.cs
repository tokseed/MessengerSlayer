using Microsoft.EntityFrameworkCore;
using Messenger.Server.Database;
using Messenger.Server.Database.Entities;
using Messenger.Shared.Models;
using Messenger.Shared.Security;

namespace Messenger.Server.Services;

public sealed class AuthService
{
    private readonly MessengerDbContext _db;

    public AuthService(MessengerDbContext db)
    {
        _db = db;
    }

    public async Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user == null)
            return new AuthResult { IsSuccess = false, ErrorMessage = "User not found" };

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            return new AuthResult { IsSuccess = false, ErrorMessage = "Invalid password" };

        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResult { IsSuccess = true, UserId = user.Id };
    }

    public async Task<AuthResult> RegisterAsync(
        string username, string password,
        string firstName, string lastName, string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Users
            .AnyAsync(u => u.Username == username || u.PhoneNumber == phoneNumber, cancellationToken);

        if (exists)
            return new AuthResult { IsSuccess = false, ErrorMessage = "Username or phone already exists" };

        var user = new User
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResult { IsSuccess = true, UserId = user.Id };
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                CreatedAt = u.CreatedAt,
                LastSeenAt = u.LastSeenAt
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class AuthResult
{
    public bool IsSuccess { get; init; }
    public int UserId { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
