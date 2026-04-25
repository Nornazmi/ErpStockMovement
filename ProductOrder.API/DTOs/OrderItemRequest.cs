namespace ProductOrder.API.DTOs;

public class OrderItemRequest
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
