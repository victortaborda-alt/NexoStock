namespace NexoStock.Application.Articles;

public sealed record CreateArticleRequest(string Code, string Name, string Description);

public sealed record ArticleResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateArticleResult(
    bool Succeeded,
    ArticleResponse? Article,
    IReadOnlyCollection<string> Errors,
    bool IsConflict);

public interface IArticleService
{
    Task<IReadOnlyCollection<ArticleResponse>> GetArticlesAsync(CancellationToken cancellationToken = default);

    Task<CreateArticleResult> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);
}
