namespace NexoStock.Application.Auth;

public sealed record LoginRequest(string UserNameOrEmail, string Password);

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string FullName,
    string Password,
    string Role = ApplicationRoles.Warehouse);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);

public sealed record CreateUserResult(
    bool Succeeded,
    UserResponse? User,
    IReadOnlyCollection<string> Errors,
    bool IsConflict);

public sealed record UserResponse(
    string Id,
    string UserName,
    string Email,
    string FullName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);
