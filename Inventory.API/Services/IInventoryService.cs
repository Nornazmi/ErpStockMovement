using Inventory.API.DTOs;
using Inventory.API.Models;

namespace Inventory.API.Services;

public interface IInventoryService
{
    Task<bool> ReserveAsync(ReserveRequest request);
    Task<InventoryItem> CreateInventoryAsync(CreateInventoryRequest request);
}
