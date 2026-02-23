using Microsoft.Extensions.Options;
using NetSuiteIntegrationAPI.Models;
using System.Text;
using System.Text.Json;

public class TypesenseService
{
    private readonly NetSuiteOptions _config;
    private readonly NetSuiteService _netSuiteService;

    public TypesenseService(
        IOptions<NetSuiteOptions> options, NetSuiteService netSuiteService)
    {
        _config = options.Value;
        _netSuiteService = netSuiteService;
    }

    private string TypesensBaseUrl => _config.TYPESENSE_BASE_URL;
    private string TypesenseAdminKey => _config.TYPESENSE_ADMIN_KEY;
    
    public async Task<string> TypesenseHealthAsync()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add(
            "X-TYPESENSE-API-KEY",
            TypesenseAdminKey
        );

        var baseUrl = TypesensBaseUrl;

        var response = await client.GetAsync($"{baseUrl}/health");
        var content = await response.Content.ReadAsStringAsync();

        return content;
    }

    public async Task<string> GenerateProductSearchSchema()
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add(
            "X-TYPESENSE-API-KEY",
            TypesenseAdminKey
        );

        // Optional: delete existing collection for clean demo reset
        await client.DeleteAsync($"{TypesensBaseUrl}/collections/products");

        var schema = new
        {
            name = "products",
            fields = new List<TypesenseField>
        {
            new() { name = "id", type = "string" },
            new() { name = "sku", type = "string" },
            new() { name = "name", type = "string" },
            new() { name = "description", type = "string" },
            new() { name = "categoryIds", type = "string[]", facet = true },
            new() { name = "price", type = "float", facet = true },
            new() { name = "quantityAvailable", type = "int32" },
            new() { name = "imageUrl", type = "string", optional = true }
        },
            default_sorting_field = "price"
        };

        var response = await client.PostAsJsonAsync(
            $"{TypesensBaseUrl}/collections",
            schema
        );

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> SyncProducts(int pageIndex, int pageSize)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        // 1️⃣ Get strongly typed data from NetSuite service
        var exportResult = await _netSuiteService.GetProductsForIndexingAsync(pageIndex, pageSize);

        if (exportResult?.Items == null || !exportResult.Items.Any())
            return "No products to index.";

        // 2️⃣ Convert to NDJSON
        var ndjson = new StringBuilder();

        foreach (var product in exportResult.Items)
        {
            var document = new
            {
                id = product.Id,
                sku = product.Sku,
                name = product.Name,
                description = product.Description ?? "",
                price = product.Price,
                quantityAvailable = product.QuantityAvailable,
                imageUrl = product.ImageUrl ?? "",
                categoryIds = product.CategoryIds ?? new List<string>(),
                lastModifiedDate = product.LastModifiedDate
            };

            var json = JsonSerializer.Serialize(document);
            ndjson.AppendLine(json);
        }

        // 3️⃣ Send to Typesense
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{TypesensBaseUrl}/collections/products/documents/import?action=upsert"
        );

        request.Headers.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        request.Content = new StringContent(
            ndjson.ToString(),
            Encoding.UTF8,
            "text/plain"
        );

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Typesense Error: {content}");

        return content;
    }

    public async Task<ProductSearchResponse> SearchProducts(
        string? query,
        int page,
        int pageSize,
        string? categoryId,
        double? minPrice,
        double? maxPrice,
        string? sort
)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        var searchParams = new List<string>
    {
        $"q={Uri.EscapeDataString(query ?? "*")}",
        "query_by=name,description,sku",
        $"page={page}",
        $"per_page={pageSize}",
        "facet_by=categoryIds",
        "max_facet_values=20"
    };

        var filters = new List<string>();

        if (!string.IsNullOrEmpty(categoryId))
            filters.Add($"categoryIds:={categoryId}");

        if (minPrice.HasValue && maxPrice.HasValue)
            filters.Add($"price:=[{minPrice},{maxPrice}]");

        if (filters.Any())
            searchParams.Add($"filter_by={string.Join(" && ", filters)}");

        searchParams.Add(!string.IsNullOrEmpty(sort)
            ? $"sort_by={sort}"
            : "sort_by=price:asc");

        var url =
            $"{TypesensBaseUrl}/collections/products/documents/search?{string.Join("&", searchParams)}";

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Typesense Search Error: {content}");

        var typesenseResult = JsonSerializer.Deserialize<TypesenseSearchResult>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (typesenseResult == null)
            throw new Exception("Failed to parse Typesense response.");

        // 🔥 Transform to Clean API Response

        var cleanResponse = new ProductSearchResponse
        {
            Total = typesenseResult.Found,
            Page = page,
            PageSize = pageSize,
            Items = typesenseResult.Hits
                .Select(h => h.Document)
                .ToList(),
            Facets = typesenseResult.Facet_Counts?
                .Select(f => new FacetDto
                {
                    Field = f.Field_Name,
                    Values = f.Counts.Select(c => new FacetValueDto
                    {
                        Value = c.Value,
                        Count = c.Count
                    }).ToList()
                }).ToList() ?? new List<FacetDto>()
        };

        return cleanResponse;
    }

}