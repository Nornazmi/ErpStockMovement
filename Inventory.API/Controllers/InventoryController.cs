using Microsoft.AspNetCore.Mvc;
using Inventory.API.DTOs;
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInventoryRequest request)
    {
        var inventoryItem = await _inventoryService.CreateInventoryAsync(request);
        return CreatedAtAction(nameof(Create), new { id = inventoryItem.Id }, inventoryItem);
    }
}
