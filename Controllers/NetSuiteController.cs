using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/netsuite")]
public class NetSuiteController : ControllerBase
{
    private readonly NetSuiteService _netSuiteService;

    public NetSuiteController(NetSuiteService netSuiteService)
    {
        _netSuiteService = netSuiteService;
    }

    [HttpGet("test-token")]
    public async Task<IActionResult> TestToken()
    {
        try
        {
            var token = await _netSuiteService.GetAccessTokenForTestAsync();
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _netSuiteService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("categories/{id}/items")]
    public async Task<IActionResult> GetCategoryItems(string id)
    {
        try
        {
            var items = await _netSuiteService.GetCategoryItemsAsync(id);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}