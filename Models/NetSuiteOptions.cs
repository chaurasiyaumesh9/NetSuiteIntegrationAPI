using System.Text.Json.Serialization;

namespace NetSuiteIntegrationAPI.Models
{
    public class NetSuiteOptions
    {
        public string AccountId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string CertificateId { get; set; } = string.Empty;
        public string TYPESENSE_ADMIN_KEY { get; set; } = string.Empty;
        public string TYPESENSE_BASE_URL { get; set; } = string.Empty;
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

    public class ProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<string> CategoryIds { get; set; } = new();

        public double Price { get; set; }
        public int QuantityAvailable { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? LastModifiedDate { get; set; }

        // 🔥 New Fields

        public string Brand { get; set; } = string.Empty;

        public double StorageCapacity { get; set; }
        public double MemoryRam { get; set; }
        public double ScreenSize { get; set; }

        public string ProcessorModel { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string NetworkType { get; set; } = string.Empty;

        public bool Featured { get; set; }

        public double CustomerRating { get; set; }
    }

    public class BulkProductResponse
    {
        public bool Success { get; set; }

        public int TotalResults { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public List<ProductDto> Items { get; set; } = new();
    }
}
