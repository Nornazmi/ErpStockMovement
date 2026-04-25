namespace Inventory.API.DTOs;

public class CreateInventoryRequest
{
    public int ProductId { get; set; }
    public int Stock { get; set; }
}
