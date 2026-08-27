using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Messenger.Server.Database;
using Messenger.Server.Database.Entities;
using Messenger.Shared.Models;
using Messenger.Shared.Security;

namespace Messenger.Server.Services;

public sealed class AuthService
{
    private const int MaximumAvatarValueLength =
        400_000;

    private readonly MessengerDbContext _db;

    public AuthService(
        MessengerDbContext db)
    {
        _db =
            db ??
            throw new ArgumentNullException(
                nameof(db));
    }

    public async Task<AuthResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        User? user =
            await _db.Users.FirstOrDefaultAsync(
                item =>
                    item.Username ==
                    username,
                cancellationToken);

        if (user == null)
        {
            return AuthResult.Failure(
                "User not found");
        }

        if (!PasswordHasher.Verify(
                password,
                user.PasswordHash))
        {
            return AuthResult.Failure(
                "Invalid password");
        }

        user.LastSeenAt =
            DateTime.UtcNow;

        await _db.SaveChangesAsync(
            cancellationToken);

        return AuthResult.Success(
            user.Id);
    }

    public async Task<AuthResult> RegisterAsync(
        string username,
        string password,
        string firstName,
        string lastName,
        string phoneNumber,
        string email,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        username =
            username.Trim();

        firstName =
            firstName.Trim();

        lastName =
            lastName.Trim();

        phoneNumber =
            phoneNumber.Trim();

        email =
            email.Trim();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(email))
        {
            return AuthResult.Failure(
                "Required fields are missing");
        }

        if (!MailAddress.TryCreate(
                email,
                out _))
        {
            return AuthResult.Failure(
                "Invalid email");
        }

        if (!IsAvatarValueValid(
                avatarUrl))
        {
            return AuthResult.Failure(
                "Invalid avatar");
        }

        bool exists =
            await _db.Users.AnyAsync(
                user =>
                    user.Username ==
                    username ||
                    user.PhoneNumber ==
                    phoneNumber ||
                    user.Email ==
                    email,
                cancellationToken);

        if (exists)
        {
            return AuthResult.Failure(
                "Username, phone or email already exists");
        }

        User user =
            new()
            {
                Username =
                    username,
                PasswordHash =
                    PasswordHasher.Hash(
                        password),
                FirstName =
                    firstName,
                LastName =
                    lastName,
                Email =
                    email,
                PhoneNumber =
                    phoneNumber,
                AvatarUrl =
                    NormalizeAvatar(
                        avatarUrl),
                CreatedAt =
                    DateTime.UtcNow,
                LastSeenAt =
                    DateTime.UtcNow
            };

        _db.Users.Add(
            user);

        await _db.SaveChangesAsync(
            cancellationToken);

        return AuthResult.Success(
            user.Id);
    }

    public async Task<UserDto?> GetUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Where(
                user =>
                    user.Id ==
                    userId)
            .Select(
                user =>
                    new UserDto
                    {
                        Id =
                            user.Id,
                        Username =
                            user.Username,
                        FirstName =
                            user.FirstName,
                        LastName =
                            user.LastName,
                        Email =
                            user.Email,
                        PhoneNumber =
                            user.PhoneNumber,
                        AvatarUrl =
                            user.AvatarUrl,
                        CreatedAt =
                            user.CreatedAt,
                        LastSeenAt =
                            user.LastSeenAt
                    })
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task<ProfileResult> UpdateProfileAsync(
        int userId,
        string email,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        email =
            email.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            !MailAddress.TryCreate(
                email,
                out _))
        {
            return ProfileResult.Failure(
                "Invalid email");
        }

        if (!IsAvatarValueValid(
                avatarUrl))
        {
            return ProfileResult.Failure(
                "Invalid avatar");
        }

        bool emailUsed =
            await _db.Users.AnyAsync(
                user =>
                    user.Id !=
                    userId &&
                    user.Email ==
                    email,
                cancellationToken);

        if (emailUsed)
        {
            return ProfileResult.Failure(
                "Email already exists");
        }

        User? user =
            await _db.Users.FirstOrDefaultAsync(
                item =>
                    item.Id ==
                    userId,
                cancellationToken);

        if (user == null)
        {
            return ProfileResult.Failure(
                "User not found");
        }

        user.Email =
            email;

        user.AvatarUrl =
            NormalizeAvatar(
                avatarUrl);

        await _db.SaveChangesAsync(
            cancellationToken);

        UserDto updated =
            new()
            {
                Id =
                    user.Id,
                Username =
                    user.Username,
                FirstName =
                    user.FirstName,
                LastName =
                    user.LastName,
                Email =
                    user.Email,
                PhoneNumber =
                    user.PhoneNumber,
                AvatarUrl =
                    user.AvatarUrl,
                CreatedAt =
                    user.CreatedAt,
                LastSeenAt =
                    user.LastSeenAt
            };

        return ProfileResult.Success(
            updated);
    }

    public async Task<List<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Select(
                user =>
                    new UserDto
                    {
                        Id =
                            user.Id,
                        Username =
                            user.Username,
                        FirstName =
                            user.FirstName,
                        LastName =
                            user.LastName,
                        Email =
                            user.Email,
                        PhoneNumber =
                            user.PhoneNumber,
                        AvatarUrl =
                            user.AvatarUrl,
                        CreatedAt =
                            user.CreatedAt,
                        LastSeenAt =
                            user.LastSeenAt
                    })
            .ToListAsync(
                cancellationToken);
    }

    private static bool IsAvatarValueValid(
        string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(
                avatarUrl))
        {
            return true;
        }

        if (avatarUrl.Length >
            MaximumAvatarValueLength)
        {
            return false;
        }

        return avatarUrl.StartsWith(
                   "data:image/png;base64,",
                   StringComparison.OrdinalIgnoreCase) ||
               avatarUrl.StartsWith(
                   "data:image/jpeg;base64,",
                   StringComparison.OrdinalIgnoreCase) ||
               avatarUrl.StartsWith(
                   "data:image/webp;base64,",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeAvatar(
        string? avatarUrl)
    {
        return string.IsNullOrWhiteSpace(
                avatarUrl)
            ? null
            : avatarUrl.Trim();
    }
}

public sealed class AuthResult
{
    public bool IsSuccess { get; init; }

    public int UserId { get; init; }

    public string ErrorMessage { get; init; } =
        string.Empty;

    public static AuthResult Success(
        int userId)
    {
        return new AuthResult
        {
            IsSuccess =
                true,
            UserId =
                userId
        };
    }

    public static AuthResult Failure(
        string error)
    {
        return new AuthResult
        {
            IsSuccess =
                false,
            ErrorMessage =
                error
        };
    }
}

public sealed class ProfileResult
{
    public bool IsSuccess { get; init; }

    public UserDto? User { get; init; }

    public string ErrorMessage { get; init; } =
        string.Empty;

    public static ProfileResult Success(
        UserDto user)
    {
        return new ProfileResult
        {
            IsSuccess =
                true,
            User =
                user
        };
    }

    public static ProfileResult Failure(
        string error)
    {
        return new ProfileResult
        {
            IsSuccess =
                false,
            ErrorMessage =
                error
        };
    }
}
