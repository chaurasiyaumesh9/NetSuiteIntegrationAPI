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
}