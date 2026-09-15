namespace NexoStock.Infrastructure.Identity;

public sealed class RolePermission
{
    public int Id { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string Permission { get; set; } = string.Empty;
}
