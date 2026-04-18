namespace Nex.Api.Data.Entities;

public class Article
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Category { get; set; } = "Product";
    public string Summary { get; set; } = "";
    public string Content { get; set; } = "";
    public string CoverImageUrl { get; set; } = "";
    public Guid AuthorId { get; set; }
    public bool IsFeatured { get; set; }
    public string Status { get; set; } = "draft"; // "draft" | "published"
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string MetaTitle { get; set; } = "";
    public string MetaDescription { get; set; } = "";
    public string Keywords { get; set; } = "";

    public User? Author { get; set; }
}

public class UploadedImage
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public Guid UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
