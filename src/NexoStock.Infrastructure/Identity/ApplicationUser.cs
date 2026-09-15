using Microsoft.AspNetCore.Identity;

namespace NexoStock.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
