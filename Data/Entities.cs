using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarkFDev.Data;

public abstract class Entity
{
    public int Id { get; set; }
}

public class SiteProfile : Entity
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string Title { get; set; } = "";
    [MaxLength(500)] public string Tagline { get; set; } = "";
    [MaxLength(100)] public string Location { get; set; } = "";
    [MaxLength(300)] public string GitHubUrl { get; set; } = "";
    [MaxLength(300)] public string LinkedInUrl { get; set; } = "";
    [MaxLength(200)] public string Email { get; set; } = "";
    // Paragraphs separated by blank lines
    [MaxLength(5000)] public string About { get; set; } = "";

    [NotMapped]
    public string[] AboutParagraphs => About.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public class SkillGroup : Entity
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public List<string> Items { get; set; } = [];
    public int SortOrder { get; set; }

    [NotMapped]
    public string ItemsText
    {
        get => string.Join(", ", Items);
        set => Items = ListText.FromComma(value);
    }
}

public class Job : Entity
{
    [Required, MaxLength(150)] public string Role { get; set; } = "";
    [Required, MaxLength(150)] public string Company { get; set; } = "";
    [MaxLength(50)] public string Period { get; set; } = "";
    [MaxLength(1000)] public string Summary { get; set; } = "";
    public List<string> Highlights { get; set; } = [];
    public List<string> Tech { get; set; } = [];
    public int SortOrder { get; set; }

    [NotMapped]
    public string HighlightsText
    {
        get => string.Join("\n", Highlights);
        set => Highlights = ListText.FromLines(value);
    }

    [NotMapped]
    public string TechText
    {
        get => string.Join(", ", Tech);
        set => Tech = ListText.FromComma(value);
    }
}

public class Project : Entity
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(1000)] public string Description { get; set; } = "";
    public List<string> Tech { get; set; } = [];
    [MaxLength(300)] public string? RepoUrl { get; set; }
    [MaxLength(300)] public string? LiveUrl { get; set; }
    public int SortOrder { get; set; }

    [NotMapped]
    public string TechText
    {
        get => string.Join(", ", Tech);
        set => Tech = ListText.FromComma(value);
    }
}

static class ListText
{
    public static List<string> FromComma(string? v) => Split(v, ',');
    public static List<string> FromLines(string? v) => Split(v, '\n');

    private static List<string> Split(string? v, char sep) =>
        (v ?? "").Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
