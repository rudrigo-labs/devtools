namespace DevTools.Core.Contracts;

public abstract class ToolEntityBase
{
    private string _id = string.Empty;

    public string Id
    {
        get => _id;
        set => _id = Utilities.SlugNormalizer.Normalize(value);
    }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

