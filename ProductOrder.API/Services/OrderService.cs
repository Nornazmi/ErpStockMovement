using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProductOrder.API.Data;
using ProductOrder.API.DTOs;
using ProductOrder.API.Models;

namespace ProductOrder.API.Services;

public class OrderService : IOrderService
{
    private readonly ProductDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private const int MaxRetryAttempts = 3;

    public OrderService(ProductDbContext context, HttpClient httpClient, IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message, int? OrderId)> CreateOrderAsync(CreateOrderRequest request)
    {
        var validationResult = ValidateOrderRequest(request);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.ErrorMessage!, null);
        }

        var productsResult = await GetProductsByIdsAsync(request.OrderItems);
        if (!productsResult.Success)
        {
            return (false, productsResult.ErrorMessage!, null);
        }

        var reservationResult = await ReserveAllInventoryAsync(request.OrderItems);
        if (!reservationResult.Success)
        {
            return (false, reservationResult.ErrorMessage!, null);
        }

        var order = await SaveOrderAsync(request.OrderItems, productsResult.Products!);

        return (true, "Order created successfully.", order.Id);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateOrderRequest(CreateOrderRequest request)
    {
        if (request.OrderItems == null || request.OrderItems.Count == 0)
        {
            return (false, "Order must contain at least one item.");
        }

        return (true, null);
    }

    private async Task<(bool Success, string? ErrorMessage, Dictionary<int, ProductItem>? Products)> GetProductsByIdsAsync(List<OrderItemRequest> orderItems)
    {
        var productIds = orderItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        foreach (var item in orderItems)
        {
            if (!products.ContainsKey(item.ProductId))
            {
                return (false, $"Product with ID {item.ProductId} does not exist.", null);
            }
        }

        return (true, null, products);
    }

    private async Task<(bool Success, string? ErrorMessage)> ReserveAllInventoryAsync(List<OrderItemRequest> orderItems)
    {
        var inventoryBaseUrl = _configuration["InventoryApi:BaseUrl"] ?? "http://localhost:5001";

        foreach (var item in orderItems)
        {
            var result = await ReserveSingleItemAsync(item, inventoryBaseUrl);
            if (!result.Success)
            {
                return result;
            }
        }

        return (true, null);
    }

    private async Task<(bool Success, string? ErrorMessage)> ReserveSingleItemAsync(OrderItemRequest item, string baseUrl)
    {
        var reserveRequest = new
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity
        };

        var json = JsonSerializer.Serialize(reserveRequest);

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{baseUrl}/api/Inventory/reserve", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Insufficient stock for product ID {item.ProductId}. Response: {responseContent}");
                }

                if (attempt == MaxRetryAttempts)
                {
                    return (false, $"Inventory API returned status {(int)response.StatusCode} after {MaxRetryAttempts} attempts");
                }
            }
            catch (HttpRequestException ex) when (attempt < MaxRetryAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }

        return (false, $"Failed to reserve stock for product ID {item.ProductId} after {MaxRetryAttempts} attempts");
    }

    private async Task<Order> SaveOrderAsync(List<OrderItemRequest> orderItems, Dictionary<int, ProductItem> products)
    {
        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            OrderItems = orderItems.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = products[item.ProductId].Price
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }
}
