using NexoStock.Application.Articles;
using NexoStock.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexoStock.Api.Controllers;

[ApiController]
[Route("api/articles")]
[Authorize(Policy = ApplicationPolicies.ArticlesRead)]
public sealed class ArticlesController(IArticleService articleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ArticleResponse>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await articleService.GetArticlesAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.ArticlesCreate)]
    public async Task<ActionResult<ArticleResponse>> Create(CreateArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await articleService.CreateArticleAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            var response = new { message = "No fue posible crear el articulo.", errors = result.Errors };
            return result.IsConflict ? Conflict(response) : BadRequest(response);
        }

        return CreatedAtAction(nameof(Get), result.Article);
    }
}
