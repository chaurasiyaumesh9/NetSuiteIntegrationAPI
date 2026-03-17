using Microsoft.AspNetCore.Mvc;
using NetSuiteIntegrationAPI.Models;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly TypesenseService _typesenseService;
    private readonly IHttpClientFactory _httpClientFactory;

    public SearchController(TypesenseService typesenseService, IHttpClientFactory httpClientFactory)
    {
        _typesenseService = typesenseService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchQuery query)
    {
        var reservedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "q", "page", "pageSize", "sort"
    };

        var filters = Request.Query
            .Where(kvp => !reservedKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        var result = await _typesenseService.SearchProducts(
            query.Q,
            query.Page,
            query.PageSize,
            filters,
            query.Sort
        );

        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var items = await _typesenseService.GetAllCategoriesAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

    }

    [HttpGet("external/items")]
    public async Task<IActionResult> GetExternalItems([FromQuery] IDictionary<string, string>? queryParams)
    {
        try
        {
            var client = _httpClientFactory != null
                ? _httpClientFactory.CreateClient()
                : new HttpClient();

            // Default parameters for the external API
            var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["c"] = "TSTDRV2206481",
                ["country"] = "US",
                ["currency"] = "USD",
                ["fieldset"] = "search",
                ["include"] = "facets",
                ["language"] = "en",
                ["limit"] = "24",
                ["n"] = "6",
                ["offset"] = "0",
                ["pricelevel"] = "5",
                ["sort"] = "relevance:desc",
                ["use_pcv"] = "F"
            };

            // Merge provided query params (override defaults)
            if (queryParams != null)
            {
                foreach (var kvp in queryParams)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                        continue;

                    defaults[kvp.Key] = kvp.Value ?? string.Empty;
                }
            }

            var queryString = string.Join('&', defaults.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            var url = $"https://sca.primarysports.com/api/items?{queryString}";

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, content);

            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategoryById(string id)
    {
        try
        {
            var item = await _typesenseService.GetCategoryByIdAsync(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}