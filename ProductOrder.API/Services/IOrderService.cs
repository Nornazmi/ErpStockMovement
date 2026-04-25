using ProductOrder.API.DTOs;

namespace ProductOrder.API.Services;

public interface IOrderService
{
    Task<(bool Success, string Message, int? OrderId)> CreateOrderAsync(CreateOrderRequest request);
}
