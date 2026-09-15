using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NexoStock.Application.Auth;
using NexoStock.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace NexoStock.Infrastructure.Authentication;

public sealed class TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
{
    public async Task<LoginResponse> CreateTokenAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpirationMinutes", 60));
        var key = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key debe configurarse y tener al menos 32 caracteres.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("full_name", user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc,
            ToResponse(user, roles));
    }

    public static UserResponse ToResponse(ApplicationUser user, IEnumerable<string> roles) =>
        new(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.FullName, user.IsActive, roles.ToArray());
}
