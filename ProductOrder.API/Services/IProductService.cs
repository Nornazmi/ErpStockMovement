using ProductOrder.API.Models;

namespace ProductOrder.API.Services;

public interface IProductService
{
    Task<IEnumerable<ProductItem>> GetAllAsync();
    Task<ProductItem?> GetByIdAsync(int id);
    Task<ProductItem> CreateAsync(CreateProductRequest request);
}
