using System.ComponentModel.DataAnnotations.Schema;

namespace ProductOrder.API.Models;

public class ProductItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }
}
