namespace ProductOrder.API.DTOs;

public class CreateOrderRequest
{
    public List<OrderItemRequest> OrderItems { get; set; } = new List<OrderItemRequest>();
}
