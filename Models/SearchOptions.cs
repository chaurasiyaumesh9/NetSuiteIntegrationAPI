namespace NetSuiteIntegrationAPI.Models
{
    public class ProductSearchResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ProductDto> Items { get; set; } = new();
        public List<FacetDto> Facets { get; set; } = new();
    }

    public class FacetDto
    {
        public string Field { get; set; } = string.Empty;
        public List<FacetValueDto> Values { get; set; } = new();
    }

    public class FacetValueDto
    {
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
