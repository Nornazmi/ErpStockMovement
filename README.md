# ERP Stock Movement System

A microservices-based stock management system built with ASP.NET Core Web API, featuring product ordering, inventory management, and HTTP-based service communication.

## Architecture

```
┌─────────────────────┐         HTTP          ┌─────────────────────┐
│   ProductOrder.API  │ ◄───────────────────► │   Inventory.API     │
│   (Port: 7117)      │      Reserve Stock    │   (Port: 7099)      │
├─────────────────────┤                       ├─────────────────────┤
│ - Product Controller│                       │ - Inventory Ctrl    │
│ - Order Controller  │                       │ - Reserve Endpoint  │
│ - Product Service   │                       │ - Inventory Service │
│ - Order Service     │                       │ - Atomic Stock Deduction
├─────────────────────┤                       ├─────────────────────┤
│ ProductOrderDb      │                       │ InventoryDb         │
│ - Products          │                       │ - InventoryItems    │
│ - Orders            │                       └─────────────────────┘
│ - OrderItems        │
└─────────────────────┘
```

### Key Design Principles

- **Service Separation**: Product/Order and Inventory are separate services with their own databases
- **HTTP Communication**: Order service calls Inventory API via HTTP (not direct DB access)
- **Atomic Stock Deduction**: Uses raw SQL with row-locking to prevent race conditions
- **Retry Logic**: Order service implements exponential backoff retry for transient failures
- **Transaction Safety**: Order is only saved if all inventory reservations succeed

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or SQL Express)
- Git

## Setup Instructions

### 1. Clone and Restore

```bash
git clone <repository-url>
cd ErpStockMovement
```

### 2. Database Setup

Run migrations for both services:

```bash
cd Inventory.API
dotnet ef database update
cd ../ProductOrder.API
dotnet ef database update
```

### 3. Run Services

**Terminal 1 - Inventory API:**
```bash
cd Inventory.API
dotnet run
```

**Terminal 2 - ProductOrder API:**
```bash
cd ProductOrder.API
dotnet run
```

### 4. Access Swagger UI

- Inventory API: `https://localhost:7099/swagger`
- ProductOrder API: `https://localhost:7117/swagger`

### 5. Configure Inventory API Connection

Since ProductOrder.API calls Inventory.API via HTTP, you must configure the correct base URL:

1. **Start Inventory API first** and note the port it runs on (check console output)
2. **Update ProductOrder.API/appsettings.json**:

```json
{
  "InventoryApi": {
    "BaseUrl": "https://localhost:7099"
  }
}
```

**Important**: The port may vary (e.g., `https://localhost:5001`, `https://localhost:7099`). Always check the actual port from the Inventory API console output and update accordingly.

## API Endpoints

### ProductOrder.API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/product` | Get all products |
| GET | `/api/product/{id}` | Get product by ID |
| POST | `/api/product` | Create new product |
| POST | `/api/order` | Create order (calls Inventory API) |

### Inventory.API

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/inventory` | Create inventory item |
| POST | `/api/inventory/reserve` | Reserve stock (atomic deduction) |

## Testing Flow

### Using .http Files (VS Code REST Client)

1. **Create Inventory** (`Inventory.API.http`):
   ```http
   POST https://localhost:7099/api/inventory
   {
     "productId": 1,
     "stock": 100
   }
   ```

2. **Create Product** (`ProductOrder.API.http`):
   ```http
   POST https://localhost:7117/api/product
   {
     "name": "Laptop",
     "price": 999.99
   }
   ```

3. **Create Order** (calls Inventory automatically):
   ```http
   POST https://localhost:7117/api/order
   {
     "orderItems": [
       {
         "productId": 1,
         "quantity": 5
       }
     ]
   }
   ```

## Retry Logic

The OrderService implements retry with exponential backoff:

```csharp
for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
{
    try {
        // Call Inventory API
    }
    catch (HttpRequestException) when (attempt < MaxRetryAttempts)
    {
        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }
}
```

- **Max Attempts**: 3
- **Backoff**: 2^attempt seconds (2s, 4s, 8s)
- **Retry Conditions**: Network timeouts, connection failures
- **No Retry**: HTTP 400 (insufficient stock), HTTP 404 (not found)

## Stock Reservation Flow

```
1. Validate order items exist
2. Validate all products exist in ProductOrderDb
3. For each item:
   a. Call Inventory API /api/inventory/reserve
   b. Retry on network failures (max 3 attempts)
   c. Fail fast on insufficient stock (400 BadRequest)
4. If all reservations succeed:
   a. Save order to ProductOrderDb
   b. Return success with order ID
5. If any reservation fails:
   a. Return error (order NOT saved)
   b. Note: No rollback needed as reservations are per-item
```

## Database Schemas

### ProductOrderDb
```sql
Products: Id, Name, Price, CreatedAt
Orders: Id, CreatedAt
OrderItems: Id, OrderId, ProductId, Quantity, Price
```

### InventoryDb
```sql
InventoryItems: Id, ProductId, Stock
```

## Configuration

### Inventory.API/appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=InventoryDb;Trusted_Connection=True;"
  }
}
```

### ProductOrder.API/appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ProductOrderDb;Trusted_Connection=True;"
  },
  "InventoryApi": {
    "BaseUrl": "https://localhost:7099"
  }
}
```

## Error Handling

| Scenario | Response |
|----------|----------|
| Empty order items | 400: "Order must contain at least one item" |
| Product not found | 400: "Product with ID X does not exist" |
| Insufficient stock | 400: "Insufficient stock for product ID X" |
| Inventory API down | 400: "Failed to reserve stock after 3 attempts" |

## Project Structure

```
ErpStockMovement/
├── Inventory.API/
│   ├── Controllers/InventoryController.cs
│   ├── Services/InventoryService.cs
│   ├── Data/InventoryDbContext.cs
│   ├── Models/InventoryItem.cs
│   └── Inventory.API.http
├── ProductOrder.API/
│   ├── Controllers/ProductController.cs
│   ├── Controllers/OrderController.cs
│   ├── Services/OrderService.cs
│   ├── Services/ProductService.cs
│   ├── Data/ProductDbContext.cs
│   └── ProductOrder.API.http
└── README.md
```

## Technologies

- ASP.NET Core 8.0 Web API
- Entity Framework Core
- SQL Server
- Polly (retry logic - custom implementation)
- Swagger/OpenAPI
