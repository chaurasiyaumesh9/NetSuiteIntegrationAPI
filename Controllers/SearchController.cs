using Microsoft.AspNetCore.Mvc;
using NetSuiteIntegrationAPI.Models;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly TypesenseService _typesenseService;

    public SearchController(TypesenseService typesenseService)
    {
        _typesenseService = typesenseService;
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