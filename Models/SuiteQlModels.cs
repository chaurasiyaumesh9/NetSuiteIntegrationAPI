namespace NetSuiteIntegrationAPI.Models;

public class SuiteQlResult<T>
{
    public List<T> Items { get; set; } = new();
}

public class SuiteQlCategoryRow
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? PrimaryParent { get; set; }
    public string? UrlFragment { get; set; }
    public string? ImageUrl { get; set; }
}