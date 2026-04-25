using Microsoft.EntityFrameworkCore;
using ProductOrder.API.Data;
using ProductOrder.API.Models;
using ProductOrder.API.DTOs;

namespace ProductOrder.API.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;

    public ProductService(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductItem>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<ProductItem?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<ProductItem> CreateAsync(CreateProductRequest request)
    {
        var product = new ProductItem
        {
            Name = request.Name,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }
}
