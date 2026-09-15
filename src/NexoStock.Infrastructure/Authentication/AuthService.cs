using NexoStock.Application.Auth;
using NexoStock.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace NexoStock.Infrastructure.Authentication;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(request.UserNameOrEmail)
                   ?? await userManager.FindByEmailAsync(request.UserNameOrEmail);

        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        return await tokenService.CreateTokenAsync(user);
    }

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!ApplicationRoles.All.Contains(request.Role))
        {
            return new CreateUserResult(false, null, new[] { "El rol solicitado no es valido." }, false);
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            EmailConfirmed = false
        };

        var creationResult = await userManager.CreateAsync(user, request.Password);
        if (!creationResult.Succeeded)
        {
            var errors = creationResult.Errors.Select(error => error.Description).ToArray();
            var isConflict = creationResult.Errors.Any(error =>
                error.Code is "DuplicateUserName" or "DuplicateEmail");
            return new CreateUserResult(false, null, errors, isConflict);
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return new CreateUserResult(false, null, roleResult.Errors.Select(error => error.Description).ToArray(), false);
        }

        return new CreateUserResult(true, TokenService.ToResponse(user, new[] { request.Role }), Array.Empty<string>(), false);
    }

    public async Task<UserResponse?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return TokenService.ToResponse(user, roles);
    }

    public async Task<IReadOnlyCollection<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<UserResponse>();
        foreach (var user in userManager.Users.OrderBy(user => user.UserName))
        {
            var roles = await userManager.GetRolesAsync(user);
            users.Add(TokenService.ToResponse(user, roles));
        }

        return users;
    }
}
