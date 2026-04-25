namespace Inventory.API.Models;

public class ReserveRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}