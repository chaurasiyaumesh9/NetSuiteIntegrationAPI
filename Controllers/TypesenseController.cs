using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/typesense")]
public class TypesenseController : ControllerBase
{
    private readonly TypesenseService _typesenseService;

    public TypesenseController(TypesenseService typesenseService)
    {
        _typesenseService = typesenseService;
    }

    [HttpGet("health")]
    public async Task<IActionResult> TypesenseHealth()
    {
        try
        {
            var token = await _typesenseService.TypesenseHealthAsync();
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("products/reset")]
    public async Task<IActionResult> GenerateProductSearchSchema()
    {
        try
        {
            var result = await _typesenseService.GenerateProductSearchSchema();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("products/sync")]
    public async Task<IActionResult> SyncProducts(int pageIndex = 0, int pageSize = 500)
    {
        try
        {
            var result = await _typesenseService.SyncProducts(pageIndex, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("categories/reset")]
    public async Task<IActionResult> GenerateCategorySearchSchema()
    {
        try
        {
            var result = await _typesenseService.GenerateCategorySearchSchema();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("categories/sync")]
    public async Task<IActionResult> SyncCategories(int pageIndex = 0, int pageSize = 500)
    {
        try
        {
            var result = await _typesenseService.SyncCategories(pageIndex, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

}