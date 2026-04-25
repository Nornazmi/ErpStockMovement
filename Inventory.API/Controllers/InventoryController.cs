using Microsoft.AspNetCore.Mvc;
using Inventory.API.Models;
using Inventory.API.Services;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveRequest request)
    {
        var result = await _inventoryService.ReserveAsync(request);

        if (!result)
        {
            return BadRequest(new { message = "Insufficient stock or product not found." });
        }

        return Ok(new { message = "Stock reserved successfully." });
    }
}
