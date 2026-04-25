using Microsoft.AspNetCore.Mvc;
using ProductOrder.API.DTOs;
using ProductOrder.API.Services;

namespace ProductOrder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var result = await _orderService.CreateOrderAsync(request);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message, orderId = result.OrderId });
    }
}
