using Inventory.API.Models;

namespace Inventory.API.Services;

public interface IInventoryService
{
    Task<bool> ReserveAsync(ReserveRequest request);
}
