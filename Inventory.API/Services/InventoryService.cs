using Inventory.API.Data;
using Inventory.API.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Services;

public class InventoryService : IInventoryService
{
    private readonly InventoryDbContext _context;

    public InventoryService(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ReserveAsync(ReserveRequest request)
    {
        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE InventoryItems SET Stock = Stock - @qty WHERE ProductId = @id AND Stock >= @qty",
            new SqlParameter("@qty", request.Quantity),
            new SqlParameter("@id", request.ProductId)
        );

        return rowsAffected > 0;
    }
}