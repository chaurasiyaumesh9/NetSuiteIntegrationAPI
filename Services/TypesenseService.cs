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

    public async Task<string> GenerateCategorySearchSchema()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        // Delete existing collection if any
        var deleteResponse = await client.DeleteAsync(
            $"{TypesensBaseUrl}/collections/categories"
        );

        if (!deleteResponse.IsSuccessStatusCode &&
            deleteResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var error = await deleteResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete categories collection: {error}");
        }

        return await CreateCategoriesCollectionAsync();
    }

    public async Task<string> CreateCategoriesCollectionAsync()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        var schema = new
        {
            name = "categories",
            fields = new object[]
            {
                new { name = "id", type = "string" },
                new { name = "name", type = "string", infix = true },
                new { name = "primaryParent", type = "string", optional = true },
                new { name = "urlFragment", type = "string", optional = true },
                new { name = "thumbnail", type = "string", optional = true },
                new { name = "featured", type = "bool", facet = true, optional = true }
            }
        };

        var response = await client.PostAsJsonAsync(
            $"{TypesensBaseUrl}/collections",
            schema
        );

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(content);

        return content;
    }

    public async Task<string> SyncCategories(int pageIndex, int pageSize)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        var exportResult = await _netSuiteService.GetCategoriesAsync();

        if (exportResult?.Items == null || !exportResult.Items.Any())
            return "No categories to index.";

        var ndjson = new StringBuilder();

        foreach (var category in exportResult.Items)
        {
            var document = new
            {
                id = category.Id,
                name = category.Name,
                primaryParent = category.PrimaryParent ?? "",
                urlFragment = category.UrlFragment ?? "",
                thumbnail = category.Thumbnail ?? "",
                featured = category.Featured
            };

            var json = JsonSerializer.Serialize(document);
            ndjson.AppendLine(json);
        }

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{TypesensBaseUrl}/collections/categories/documents/import?action=upsert"
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

    public async Task<string> GenerateProductSearchSchema()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        // 1️⃣ Delete collection if exists
        var deleteResponse = await client.DeleteAsync(
            $"{TypesensBaseUrl}/collections/products"
        );

        // Ignore 404 (collection not found)
        if (!deleteResponse.IsSuccessStatusCode &&
            deleteResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var error = await deleteResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete collection: {error}");
        }

        // 2️⃣ Recreate with new schema
        return await CreateProductsCollectionAsync();
    }

    public async Task<string> CreateProductsCollectionAsync()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        var schema = new
        {
            name = "products",
            fields = new object[]
            {
            new { name = "id", type = "string" },
            new { name = "sku", type = "string" },
            new { name = "name", type = "string", infix = true },
            new { name = "description", type = "string", infix = true },

            new { name = "categoryIds", type = "string[]", facet = true },

            new { name = "price", type = "float", facet = true },
            new { name = "quantityAvailable", type = "int32" },
            new { name = "imageUrl", type = "string", optional = true },
            new { name = "lastModifiedDate", type = "string", optional = true },

            // 🔥 NEW FIELDS

            new { name = "brand", type = "string", facet = true , optional = true},
            new { name = "storageCapacity", type = "float", facet = true , optional = true},
            new { name = "memoryRam", type = "float", facet = true , optional = true},
            new { name = "screenSize", type = "float", facet = true , optional = true},

            new { name = "processorModel", type = "string", infix = true , optional = true},
            new { name = "color", type = "string", facet = true , optional = true},
            new { name = "networkType", type = "string", facet = true , optional = true},

            new { name = "featured", type = "bool", facet = true, optional = true},
            new { name = "customerRating", type = "float", facet = true, optional = true }
            },
            default_sorting_field = "price"
        };

        var response = await client.PostAsJsonAsync(
            $"{TypesensBaseUrl}/collections",
            schema
        );

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(content);

        return content;
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
                description = product.Description,
                categoryIds = product.CategoryIds ?? new List<string>(),
                price = product.Price,
                quantityAvailable = product.QuantityAvailable,
                imageUrl = product.ImageUrl ?? "",
                lastModifiedDate = product.LastModifiedDate,

                // 🔥 THESE MUST EXIST

                brand = product.Brand ?? "",
                storageCapacity = product.StorageCapacity,
                memoryRam = product.MemoryRam,
                screenSize = product.ScreenSize,
                processorModel = product.ProcessorModel ?? "",
                color = product.Color ?? "",
                networkType = product.NetworkType ?? "",
                featured = product.Featured,
                customerRating = product.CustomerRating
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

    private static string BuildFilterExpression(IDictionary<string, string>? filters)
    {
        if (filters == null || !filters.Any())
            return string.Empty;

        var expressions = new List<string>();

        foreach (var kvp in filters)
        {
            var field = kvp.Key;
            var rawValue = kvp.Value;

            if (string.IsNullOrWhiteSpace(rawValue))
                continue;

            var tokens = rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => t.Trim())
                                 .Where(t => !string.IsNullOrWhiteSpace(t))
                                 .ToList();

            if (!tokens.Any())
                continue;

            // Detect boolean
            if (tokens.Count == 1 && bool.TryParse(tokens[0], out _))
            {
                expressions.Add($"{field}:={tokens[0].ToLower()}");
                continue;
            }

            // Detect numeric or numeric range
            bool isNumericOrRange = tokens.All(t =>
                t.Contains('-')
                    ? t.Split('-', StringSplitOptions.RemoveEmptyEntries)
                       .All(p => double.TryParse(p, out _))
                    : double.TryParse(t, out _)
            );

            if (isNumericOrRange)
            {
                // Single range like 10-20
                if (tokens.Count == 1 && tokens[0].Contains('-'))
                {
                    var parts = tokens[0].Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        expressions.Add($"{field}:=[{parts[0]},{parts[1]}]");
                        continue;
                    }
                }

                // Single numeric
                if (tokens.Count == 1)
                    expressions.Add($"{field}:={tokens[0]}");
                else
                    expressions.Add($"{field}:=[{string.Join(",", tokens)}]");

                continue;
            }

            // Otherwise treat as string facet
            if (tokens.Count == 1)
                expressions.Add($"{field}:={tokens[0]}");
            else
                expressions.Add($"{field}:=[{string.Join(",", tokens)}]");
        }

        return string.Join(" && ", expressions);
    }

    public async Task<ProductSearchResponse> SearchProducts(
    string? query,
    int page,
    int pageSize,
    IDictionary<string, string>? filters = null,
    string? sort = null
)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        var searchParams = new List<string>
    {
        $"q={Uri.EscapeDataString(query ?? "*")}",
        "query_by=name,description,sku",
        $"page={page}",
        $"per_page={pageSize}",
        "facet_by=categoryIds,brand,color,networkType,storageCapacity,memoryRam,screenSize,customerRating,featured",
        "max_facet_values=20"
    };

        var filterExpression = BuildFilterExpression(filters);

        if (!string.IsNullOrEmpty(filterExpression))
            searchParams.Add($"filter_by={filterExpression}");

        searchParams.Add(!string.IsNullOrEmpty(sort)
            ? $"sort_by={sort}"
            : "sort_by=price:asc");

        var url = $"{TypesensBaseUrl}/collections/products/documents/search?{string.Join("&", searchParams)}";

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Typesense Search Error: {content}");

        var typesenseResult = JsonSerializer.Deserialize<TypesenseSearchResult>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new Exception("Failed to parse Typesense response.");

        return new ProductSearchResponse
        {
            Total = typesenseResult.Found,
            Page = page,
            PageSize = pageSize,
            Items = typesenseResult.Hits.Select(h => h.Document).ToList(),
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
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        var url = $"{TypesensBaseUrl}/collections/categories/documents/search?q={Uri.EscapeDataString("*")}&query_by=name&per_page=200";

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Typesense Search Error: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var results = new List<CategoryDto>();

        if (root.TryGetProperty("hits", out var hits) && hits.ValueKind == JsonValueKind.Array)
        {
            foreach (var hit in hits.EnumerateArray())
            {
                if (hit.TryGetProperty("document", out var document))
                {
                    var cat = new CategoryDto
                    {
                        Id = document.GetProperty("id").GetString() ?? string.Empty,
                        Name = document.GetProperty("name").GetString() ?? string.Empty,
                        PrimaryParent = document.TryGetProperty("primaryParent", out var pp) ? pp.GetString() ?? string.Empty : string.Empty,
                        UrlFragment = document.TryGetProperty("urlFragment", out var uf) ? uf.GetString() ?? string.Empty : string.Empty,
                        Thumbnail = document.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? string.Empty : string.Empty,
                        Featured = document.TryGetProperty("featured", out var ft) && ft.ValueKind == JsonValueKind.True
                    };

                    results.Add(cat);
                }
            }
        }

        return results;
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(string id)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", TypesenseAdminKey);

        var url = $"{TypesensBaseUrl}/collections/categories/documents/{Uri.EscapeDataString(id)}";

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Typesense Error: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var cat = new CategoryDto
        {
            Id = root.GetProperty("id").GetString() ?? string.Empty,
            Name = root.GetProperty("name").GetString() ?? string.Empty,
            PrimaryParent = root.TryGetProperty("primaryParent", out var pp) ? pp.GetString() ?? string.Empty : string.Empty,
            UrlFragment = root.TryGetProperty("urlFragment", out var uf) ? uf.GetString() ?? string.Empty : string.Empty,
            Thumbnail = root.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? string.Empty : string.Empty,
            Featured = root.TryGetProperty("featured", out var ft) && ft.ValueKind == JsonValueKind.True
        };

        return cat;
    }

}