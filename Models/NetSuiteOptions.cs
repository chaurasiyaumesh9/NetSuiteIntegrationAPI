using System.Text.Json.Serialization;

namespace NetSuiteIntegrationAPI.Models
{
    public class NetSuiteOptions
    {
        public string AccountId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string CertificateId { get; set; } = string.Empty;
    }

    public class NsToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ExpiresIn { get; set; }
    }

    public class CategoriesResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<CategoryDto> Items { get; set; } = new();
    }

    public class CategoryDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string PrimaryParent { get; set; } = default!;
        public string UrlFragment { get; set; } = default!;
        public string Thumbnail { get; set; } = default!;
    }

    public class CategoryItemsResponse
    {
        public bool Success { get; set; } = default!;
        public string CategoryId { get; set; } = default!;
        public int TotalResults { get; set; } = default!;
        public int PageIndex { get; set; } = default!;
        public int PageSize { get; set; } = default!;
        public List<CategoryItemDto> Items { get; set; } = new();
    }

    public class CategoryItemDto
    {
        public string Id { get; set; } = default!;
        public string Sku { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Price { get; set; } = default!;
        public string ImageUrl { get; set; } = default!;
        public string QuantityAvailable { get; set; } = default!;
    }
}
