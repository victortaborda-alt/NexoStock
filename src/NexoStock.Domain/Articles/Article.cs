namespace NexoStock.Domain.Articles;

public sealed class Article
{
    private Article()
    {
    }

    public Article(string code, string name, string description)
    {
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
