using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> SearchProducts(
        string? q,
        int page = 1,
        int pageSize = 12,
        string? categoryId = null,
        double? minPrice = null,
        double? maxPrice = null,
        string? sort = null
    )
    {
        var result = await _typesenseService.SearchProducts(
            q, page, pageSize, categoryId, minPrice, maxPrice, sort);

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