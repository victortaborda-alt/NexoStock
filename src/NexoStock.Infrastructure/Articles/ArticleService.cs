using NexoStock.Application.Articles;
using NexoStock.Domain.Articles;
using NexoStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NexoStock.Infrastructure.Articles;

public sealed class ArticleService(ApplicationDbContext dbContext) : IArticleService
{
    public async Task<IReadOnlyCollection<ArticleResponse>> GetArticlesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Articles
            .AsNoTracking()
            .OrderBy(article => article.Code)
            .Select(article => new ArticleResponse(
                article.Id,
                article.Code,
                article.Name,
                article.Description,
                article.IsActive,
                article.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CreateArticleResult> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        var description = request.Description.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return new CreateArticleResult(false, null, new[] { "El codigo y el nombre son obligatorios." }, false);
        }

        if (await dbContext.Articles.AnyAsync(article => article.Code == code, cancellationToken))
        {
            return new CreateArticleResult(false, null, new[] { "Ya existe un articulo con ese codigo." }, true);
        }

        var article = new Article(code, name, description);
        dbContext.Articles.Add(article);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateArticleResult(
            true,
            new ArticleResponse(article.Id, article.Code, article.Name, article.Description, article.IsActive, article.CreatedAtUtc),
            Array.Empty<string>(),
            false);
    }
}
