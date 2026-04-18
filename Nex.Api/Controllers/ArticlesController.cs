using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nex.Api.Data;
using Nex.Api.Data.Entities;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ArticlesController> _logger;
    private readonly IOpenAIDirectClient _openAIClient;

    public ArticlesController(
        AppDbContext db,
        IConfiguration configuration,
        ILogger<ArticlesController> logger,
        IOpenAIDirectClient openAIClient)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
        _openAIClient = openAIClient;
    }

    private bool IsCallerAdmin =>
        User.Identity?.IsAuthenticated == true &&
        User.Claims.Any(c => c.Type == "isAdmin" && c.Value == "true");

    private Guid? GetCallerId()
    {
        var raw = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    // ─── Public endpoints ───────────────────────────────────────────────────

    [HttpGet("published")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublished()
    {
        var articles = await _db.Articles
            .Where(a => a.Status == "published" && a.PublishedAt != null)
            .OrderByDescending(a => a.PublishedAt)
            .Include(a => a.Author)
            .Select(a => new
            {
                id = a.Id,
                title = a.Title,
                slug = a.Slug,
                category = a.Category,
                summary = a.Summary,
                coverImageUrl = a.CoverImageUrl,
                authorName = a.Author != null ? a.Author.DisplayName : "MetaMed",
                isFeatured = a.IsFeatured,
                publishedAt = a.PublishedAt,
            })
            .ToListAsync();

        return Ok(articles);
    }

    [HttpGet("published/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedBySlug(string slug)
    {
        var article = await _db.Articles
            .Where(a => a.Slug == slug && a.Status == "published")
            .Include(a => a.Author)
            .FirstOrDefaultAsync();

        if (article == null) return NotFound();

        return Ok(new
        {
            id = article.Id,
            title = article.Title,
            slug = article.Slug,
            category = article.Category,
            summary = article.Summary,
            content = article.Content,
            coverImageUrl = article.CoverImageUrl,
            authorName = article.Author?.DisplayName ?? "MetaMed",
            isFeatured = article.IsFeatured,
            publishedAt = article.PublishedAt,
            createdAt = article.CreatedAt,
            metaTitle = article.MetaTitle,
            metaDescription = article.MetaDescription,
            keywords = article.Keywords,
        });
    }

    // ─── Admin endpoints ────────────────────────────────────────────────────

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        if (!IsCallerAdmin) return Forbid();

        var articles = await _db.Articles
            .OrderByDescending(a => a.UpdatedAt)
            .Include(a => a.Author)
            .Select(a => new
            {
                id = a.Id,
                title = a.Title,
                slug = a.Slug,
                category = a.Category,
                summary = a.Summary,
                coverImageUrl = a.CoverImageUrl,
                status = a.Status,
                isFeatured = a.IsFeatured,
                authorName = a.Author != null ? a.Author.DisplayName : "Unknown",
                publishedAt = a.PublishedAt,
                createdAt = a.CreatedAt,
                updatedAt = a.UpdatedAt,
            })
            .ToListAsync();

        return Ok(articles);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!IsCallerAdmin) return Forbid();

        var article = await _db.Articles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null) return NotFound();

        return Ok(new
        {
            id = article.Id,
            title = article.Title,
            slug = article.Slug,
            category = article.Category,
            summary = article.Summary,
            content = article.Content,
            coverImageUrl = article.CoverImageUrl,
            status = article.Status,
            isFeatured = article.IsFeatured,
            authorName = article.Author?.DisplayName ?? "Unknown",
            publishedAt = article.PublishedAt,
            createdAt = article.CreatedAt,
            updatedAt = article.UpdatedAt,
            metaTitle = article.MetaTitle,
            metaDescription = article.MetaDescription,
            keywords = article.Keywords,
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ArticleRequest request)
    {
        if (!IsCallerAdmin) return Forbid();
        var callerId = GetCallerId();
        if (callerId == null) return Unauthorized();

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Title)
            : GenerateSlug(request.Slug);

        var existing = await _db.Articles.AnyAsync(a => a.Slug == slug);
        if (existing) slug = $"{slug}-{DateTime.UtcNow.Ticks % 100000}";

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = request.Title?.Trim() ?? "",
            Slug = slug,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Briefing" : request.Category!.Trim(),
            Summary = request.Summary?.Trim() ?? "",
            Content = request.Content ?? "",
            CoverImageUrl = request.CoverImageUrl ?? "",
            AuthorId = callerId.Value,
            IsFeatured = request.IsFeatured ?? false,
            Status = request.Status == "published" ? "published" : "draft",
            PublishedAt = request.Status == "published" ? (request.PublishedAt ?? DateTime.UtcNow) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            MetaTitle = request.MetaTitle?.Trim() ?? "",
            MetaDescription = request.MetaDescription?.Trim() ?? "",
            Keywords = request.Keywords?.Trim() ?? "",
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();

        return Ok(new { id = article.Id, slug = article.Slug });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] ArticleRequest request)
    {
        if (!IsCallerAdmin) return Forbid();

        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();

        article.Title = request.Title?.Trim() ?? article.Title;
        article.Summary = request.Summary?.Trim() ?? article.Summary;
        article.Content = request.Content ?? article.Content;
        article.CoverImageUrl = request.CoverImageUrl ?? article.CoverImageUrl;
        article.IsFeatured = request.IsFeatured ?? article.IsFeatured;
        article.MetaTitle = request.MetaTitle?.Trim() ?? article.MetaTitle;
        article.MetaDescription = request.MetaDescription?.Trim() ?? article.MetaDescription;
        article.Keywords = request.Keywords?.Trim() ?? article.Keywords;
        article.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Category))
            article.Category = request.Category!.Trim();

        if (!string.IsNullOrWhiteSpace(request.Slug) && request.Slug != article.Slug)
        {
            var newSlug = GenerateSlug(request.Slug);
            var exists = await _db.Articles.AnyAsync(a => a.Slug == newSlug && a.Id != id);
            if (!exists) article.Slug = newSlug;
        }

        var wasPublished = article.Status == "published";
        article.Status = request.Status == "published" ? "published" : "draft";
        if (article.Status == "published")
        {
            if (request.PublishedAt.HasValue)
                article.PublishedAt = request.PublishedAt.Value;
            else if (!wasPublished)
                article.PublishedAt = DateTime.UtcNow;
        }
        else
        {
            article.PublishedAt = null;
        }

        await _db.SaveChangesAsync();
        return Ok(new { id = article.Id, slug = article.Slug });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!IsCallerAdmin) return Forbid();

        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = true });
    }

    // ─── Image upload ────────────────────────────────────────────────────────

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (!IsCallerAdmin) return Forbid();
        var callerId = GetCallerId();
        if (callerId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Invalid file type" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var image = new UploadedImage
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            Data = ms.ToArray(),
            UploadedBy = callerId.Value,
            CreatedAt = DateTime.UtcNow,
        };

        _db.UploadedImages.Add(image);
        await _db.SaveChangesAsync();

        var url = BuildImageUrl(image.Id);
        return Ok(new { url });
    }

    [HttpGet("images/{id:guid}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetImage(Guid id)
    {
        var image = await _db.UploadedImages.FindAsync(id);
        if (image == null) return NotFound();
        return File(image.Data, image.ContentType, image.FileName);
    }

    // ─── AI Generate ─────────────────────────────────────────────────────────

    [HttpPost("generate")]
    [Authorize]
    public async Task<IActionResult> GenerateArticle([FromBody] GenerateArticleRequest request)
    {
        if (!IsCallerAdmin) return Forbid();
        var callerId = GetCallerId();
        if (callerId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Topic))
            return BadRequest(new { error = "Topic is required" });

        if (!_openAIClient.IsConfigured)
            return StatusCode(503, new { error = "AI generation is not configured. Set OPENAI_API_KEY or AZURE_OPENAI_KEY." });

        try
        {
            var tone = string.IsNullOrWhiteSpace(request.Tone) ? "professional clinical" : request.Tone!.Trim();
            var length = string.IsNullOrWhiteSpace(request.Length) ? "long" : request.Length!.Trim().ToLowerInvariant();
            var wordTarget = length switch
            {
                "short" => "500-700",
                "medium" => "900-1200",
                _ => "1400-1800"
            };

            var systemPrompt = GetIpcAgentSystemPrompt();
            var userPrompt = $@"Write an evidence-based article for an infection prevention and control audience about: {request.Topic}

Tone: {tone}
Target length: {wordTarget} words
Audience: IPC leads, microbiologists, antimicrobial stewardship pharmacists, hospital epidemiologists.

Use web search to find the latest peer-reviewed evidence, surveillance data, and guideline updates. Cite credible sources inline.";

            var responsesResult = await _openAIClient.ResponsesAsync(
                model: "gpt-4o",
                instructions: systemPrompt,
                userPrompt: userPrompt,
                enableWebSearch: true,
                temperature: 0.55,
                timeout: TimeSpan.FromMinutes(5));

            if (!responsesResult.IsSuccess)
            {
                _logger.LogError("Article generation failed: {Status} {Body}",
                    responsesResult.StatusCode,
                    responsesResult.ResponseBody[..Math.Min(500, responsesResult.ResponseBody.Length)]);
                return StatusCode(500, new { error = "AI generation failed. Please try again." });
            }

            var articleJson = ExtractArticleJson(responsesResult.ResponseBody);
            if (articleJson == null)
                return StatusCode(500, new { error = "Failed to parse AI response." });

            var slug = GenerateSlug(articleJson.Title);
            var existingSlug = await _db.Articles.AnyAsync(a => a.Slug == slug);
            if (existingSlug) slug = $"{slug}-{DateTime.UtcNow.Ticks % 100000}";

            string coverImageUrl = "";
            if (request.GenerateImage)
            {
                try
                {
                    var imagePrompt = BuildImagePromptForArticle(articleJson.Title ?? request.Topic);
                    var imgResult = await _openAIClient.GenerateImageAsync(imagePrompt);
                    var image = new UploadedImage
                    {
                        Id = Guid.NewGuid(),
                        FileName = $"cover-{slug}.png",
                        ContentType = imgResult.ContentType,
                        Data = imgResult.Data,
                        UploadedBy = callerId.Value,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _db.UploadedImages.Add(image);
                    await _db.SaveChangesAsync();
                    coverImageUrl = BuildImageUrl(image.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cover image failed, continuing without image");
                }
            }

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = CleanText(articleJson.Title?.Trim() ?? "Untitled"),
                Slug = slug,
                Category = string.IsNullOrWhiteSpace(request.Category) ? "Briefing" : request.Category!.Trim(),
                Summary = CleanText(articleJson.Summary?.Trim() ?? ""),
                Content = CleanContent(articleJson.Content ?? ""),
                CoverImageUrl = coverImageUrl,
                AuthorId = callerId.Value,
                IsFeatured = false,
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                MetaTitle = CleanText(articleJson.MetaTitle?.Trim() ?? ""),
                MetaDescription = CleanText(articleJson.MetaDescription?.Trim() ?? ""),
                Keywords = articleJson.Keywords?.Trim() ?? "",
            };

            _db.Articles.Add(article);
            await _db.SaveChangesAsync();

            _logger.LogInformation("AI generated article '{Title}' (id={Id})", article.Title, article.Id);
            return Ok(new { id = article.Id, slug = article.Slug, title = article.Title });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Article generation failed");
            return StatusCode(500, new { error = $"Generation failed: {ex.Message}" });
        }
    }

    [HttpPost("{id:guid}/regenerate-image")]
    [Authorize]
    public async Task<IActionResult> RegenerateImage(Guid id)
    {
        if (!IsCallerAdmin) return Forbid();
        var callerId = GetCallerId();
        if (callerId == null) return Unauthorized();

        if (!_openAIClient.IsConfigured)
            return StatusCode(503, new { error = "AI is not configured." });

        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();

        try
        {
            var prompt = BuildImagePromptForArticle(article.Title);
            var imgResult = await _openAIClient.GenerateImageAsync(prompt);
            var image = new UploadedImage
            {
                Id = Guid.NewGuid(),
                FileName = $"cover-{article.Slug}.png",
                ContentType = imgResult.ContentType,
                Data = imgResult.Data,
                UploadedBy = callerId.Value,
                CreatedAt = DateTime.UtcNow,
            };
            _db.UploadedImages.Add(image);
            article.CoverImageUrl = BuildImageUrl(image.Id);
            article.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { coverImageUrl = article.CoverImageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image regeneration failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private string BuildImageUrl(Guid imageId)
    {
        var publicBase = _configuration["PublicApiUrl"];
        if (!string.IsNullOrWhiteSpace(publicBase))
            return $"{publicBase!.TrimEnd('/')}/api/articles/images/{imageId}";
        return $"{Request.Scheme}://{Request.Host}/api/articles/images/{imageId}";
    }

    private static string BuildImagePromptForArticle(string title) =>
        $"A clean, modern editorial illustration for a healthcare and infection prevention magazine cover article titled '{title}'. " +
        "Minimalist, scientific style. Muted greys, sterile whites, a single accent of neon sky blue. " +
        "Subjects may include hospital corridors, microscopy of bacteria, lab glassware, gloved hands, surveillance dashboards, or molecular structures. " +
        "Documentary, photojournalistic feel. No text overlays, no logos, no people in identifiable detail.";

    private static string GetIpcAgentSystemPrompt() =>
        @"You are a senior medical writer with 15+ years covering infection prevention and control, antimicrobial resistance, and hospital epidemiology for outlets like The Lancet Infectious Diseases, BMJ, and Eurosurveillance. You now write for MetaMed, a clinical intelligence platform used by NHS, ECDC, and CDC affiliated IPC teams.

Your task: research the given topic using web search, then write a high-quality, evidence-led article tailored to clinical infection prevention leaders.

WRITING STYLE — CRITICAL (must read as human-written):
- Write like a clinical journalist, not an AI. Direct, precise, opinionated where evidence allows.
- Vary sentence length. Mix short factual sentences with longer analytic ones.
- NEVER use AI-tell phrases: 'In today's rapidly evolving...', 'It's worth noting that...', 'In conclusion...', 'Let's dive in...', 'Here's what you need to know', 'The landscape of...', 'Navigate the complexities...', 'Only time will tell...', 'At the end of the day...', 'In the realm of...', 'delve into', 'leverage' (as a verb), 'utilize' (use 'use' instead), 'crucial', 'pivotal', 'game-changer', 'paradigm shift'.
- NEVER use em dashes (—). Use commas, periods or parentheses.
- NEVER use 'underscores' as a verb.
- Avoid starting consecutive paragraphs with the same word.
- Do not over-use 'Furthermore', 'Moreover', 'Additionally'.
- Use contractions naturally ('it's', 'don't', 'we've').
- Anchor the lede with a specific statistic, date, or pathogen.

CONTENT REQUIREMENTS:
1. Use web search to find the latest peer-reviewed evidence and surveillance updates (UKHSA / ESPAUR, ECDC, CDC, WHO, JAC, ICHE, CID, CMI, JHI).
2. Write in clean HTML using <h2>, <h3>, <p>, <blockquote>, <ul>, <li>, <strong>, <em>.
3. Include 4-6 inline citation links as <a href=""URL"" target=""_blank"" rel=""noopener"">source name</a>. Cite UKHSA, ECDC, CDC, peer-reviewed journals, NICE, NHS England guidance.
4. Structure: clinical lede with specific data; 3-5 sections with informative H2 headings; an 'Implications for IPC teams' or 'What this means in practice' section near the end; no marketing fluff.
5. Include real metrics: incidence, resistance rates, ORs, hazard ratios, p-values where reported.
6. Be technically correct on AMR, MDRO terminology, surveillance methodology.
7. End with a forward-looking 2-3 sentence outlook for the next 6-12 months.

SEO REQUIREMENTS:
- metaTitle: max 60 chars, include primary keyword (e.g. 'CPE', 'MRSA', 'AMR', 'C. difficile').
- metaDescription: max 155 chars, compelling.
- keywords: 5-8 comma-separated.

OUTPUT FORMAT - Return ONLY valid JSON, no markdown code fences:
{
  ""title"": ""Specific clinical headline (50-70 chars)"",
  ""slug"": ""url-friendly-slug"",
  ""summary"": ""2-3 sentence summary for cards (max 220 chars)"",
  ""metaTitle"": ""SEO title (max 60 chars)"",
  ""metaDescription"": ""SEO meta description (max 155 chars)"",
  ""keywords"": ""keyword1, keyword2, keyword3"",
  ""content"": ""<h2>Section title</h2><p>Body with <a href='https://...' target='_blank' rel='noopener'>source</a>.</p>...""
}";

    internal record ParsedArticleJson(
        string? Title, string? Slug, string? Summary,
        string? MetaTitle, string? MetaDescription, string? Keywords,
        string? Content);

    internal static ParsedArticleJson? ExtractArticleJson(string raw)
    {
        try
        {
            string jsonText = raw;
            try
            {
                var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in output.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out var t) && t.GetString() == "message")
                        {
                            if (item.TryGetProperty("content", out var content))
                            {
                                foreach (var part in content.EnumerateArray())
                                {
                                    if (part.TryGetProperty("type", out var pt) && pt.GetString() == "output_text")
                                    {
                                        jsonText = part.GetProperty("text").GetString() ?? raw;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            jsonText = jsonText.Trim();
            if (jsonText.StartsWith("```")) jsonText = Regex.Replace(jsonText, @"^```\w*\s*", "");
            if (jsonText.EndsWith("```")) jsonText = jsonText[..^3];
            jsonText = jsonText.Trim();

            var start = jsonText.IndexOf('{');
            var end = jsonText.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            jsonText = jsonText[start..(end + 1)];

            var parsed = JsonDocument.Parse(jsonText);
            var root = parsed.RootElement;

            return new ParsedArticleJson(
                root.TryGetProperty("title", out var ti) ? ti.GetString() : null,
                root.TryGetProperty("slug", out var sl) ? sl.GetString() : null,
                root.TryGetProperty("summary", out var su) ? su.GetString() : null,
                root.TryGetProperty("metaTitle", out var mt) ? mt.GetString() : null,
                root.TryGetProperty("metaDescription", out var md) ? md.GetString() : null,
                root.TryGetProperty("keywords", out var kw) ? kw.GetString() : null,
                root.TryGetProperty("content", out var co) ? co.GetString() : null
            );
        }
        catch
        {
            return null;
        }
    }

    internal static string CleanContent(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;

        html = html.Replace(" — ", ", ").Replace("—", ",").Replace(" – ", ", ").Replace("–", "-");
        html = html.Replace("\u201C", "\"").Replace("\u201D", "\"");
        html = html.Replace("\u2018", "'").Replace("\u2019", "'");
        html = html.Replace("\u2026", "...");

        var aiPhrases = new[]
        {
            @"In today's rapidly evolving[\w\s]*,\s*",
            @"It'?s worth noting that\s*",
            @"In conclusion,?\s*",
            @"Let'?s dive in\.?\s*",
            @"Here'?s what you need to know\.?\s*",
            @"In the (ever[- ])?evolving landscape of[\w\s]*,\s*",
            @"Navigate the complexities of[\w\s]*",
            @"At the end of the day,?\s*",
            @"In the realm of[\w\s]*,\s*",
            @"Only time will tell[\w\s]*\.?\s*",
            @"It remains to be seen[\w\s]*\.?\s*",
        };
        foreach (var phrase in aiPhrases)
            html = Regex.Replace(html, phrase, "", RegexOptions.IgnoreCase);

        html = Regex.Replace(html, @"\bdelve[sd]?\b", "look", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\butilize[sd]?\b", "use", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\butilizing\b", "using", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\bpivotal\b", "important", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\bgame[- ]changer\b", "major shift", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\bparadigm shift\b", "major change", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\bunderscores\b", "highlights", RegexOptions.IgnoreCase);

        html = Regex.Replace(html, @"<p>\s*(Furthermore|Moreover|Additionally|Consequently),?\s*", "<p>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\s{2,}", " ");
        html = Regex.Replace(html, @"<p>\s*</p>", "");

        return html.Trim();
    }

    internal static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Replace(" — ", ", ").Replace("—", ",").Replace(" – ", ", ").Replace("–", "-");
        text = text.Replace("\u201C", "\"").Replace("\u201D", "\"");
        text = text.Replace("\u2018", "'").Replace("\u2019", "'");
        text = text.Replace("\u2026", "...");
        return text.Trim();
    }

    internal static string GenerateSlug(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return $"article-{Guid.NewGuid().ToString()[..8]}";
        var slug = title.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? $"article-{Guid.NewGuid().ToString()[..8]}" : slug;
    }
}

public class ArticleRequest
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Category { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Status { get; set; }
    public bool? IsFeatured { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? Keywords { get; set; }
}

public class GenerateArticleRequest
{
    public string Topic { get; set; } = "";
    public string? Tone { get; set; }
    public string? Length { get; set; }
    public string? Category { get; set; }
    public bool GenerateImage { get; set; } = true;
}
