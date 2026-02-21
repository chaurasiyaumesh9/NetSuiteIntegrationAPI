namespace NetSuiteIntegrationAPI.Models;

public class CategoryDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string? Image { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
}