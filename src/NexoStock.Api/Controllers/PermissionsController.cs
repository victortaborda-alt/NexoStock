using NexoStock.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexoStock.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = ApplicationPolicies.UsersCreate)]
public sealed class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RolePermissionsResponse>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await permissionService.GetRolePermissionsAsync(cancellationToken));
    }

    [HttpPut("{role}")]
    public async Task<ActionResult<RolePermissionsResponse>> Update(
        string role,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await permissionService.UpdateRolePermissionsAsync(role, request, cancellationToken);
        return response is null
            ? BadRequest(new { message = "El rol o uno de los permisos no es valido." })
            : Ok(response);
    }
}
