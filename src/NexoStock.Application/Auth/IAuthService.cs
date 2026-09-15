namespace NexoStock.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default);
}

public static class ApplicationRoles
{
    public const string Administrator = "Administrador";
    public const string Warehouse = "Bodeguero";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Administrator,
            Warehouse
        };
}

public static class ApplicationPolicies
{
    public const string ProfileRead = "Profile.Read";
    public const string UsersRead = "Users.Read";
    public const string UsersCreate = "Users.Create";
    public const string ArticlesRead = "Articles.Read";
    public const string ArticlesCreate = "Articles.Create";
}

public static class ApplicationPermissions
{
    public const string ProfileRead = ApplicationPolicies.ProfileRead;
    public const string UsersRead = ApplicationPolicies.UsersRead;
    public const string UsersCreate = ApplicationPolicies.UsersCreate;
    public const string ArticlesRead = ApplicationPolicies.ArticlesRead;
    public const string ArticlesCreate = ApplicationPolicies.ArticlesCreate;

    public static readonly IReadOnlyCollection<string> All =
        new[] { ProfileRead, UsersRead, UsersCreate, ArticlesRead, ArticlesCreate };
}

public sealed record RolePermissionsResponse(string Role, IReadOnlyCollection<string> Permissions);

public sealed record UpdateRolePermissionsRequest(IReadOnlyCollection<string> Permissions);

public interface IPermissionService
{
    Task<IReadOnlyCollection<RolePermissionsResponse>> GetRolePermissionsAsync(CancellationToken cancellationToken = default);

    Task<RolePermissionsResponse?> UpdateRolePermissionsAsync(string role, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default);
}
