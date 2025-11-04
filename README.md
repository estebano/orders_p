# Order Management API

A .NET 8.0 web API demonstrating CRUD operations for orders with JWT authentication and multiple storage options.

## Features

- REST API with CRUD operations for orders
- JWT Bearer token authentication
- Multiple storage backends:
  - In-memory (ConcurrentDictionary for thread safety)
  - SQLite with Entity Framework Core
- Integration tests including concurrent operation tests
- Configurable logging

## Prerequisites

- .NET SDK 8.0 or later
- (Optional) Postman for API testing

## Project Structure

```
OrderApp/              # Main application
├── Models/           # Data models
├── Services/         # Business logic
├── Repositories/     # Data access
├── Data/            # EF Core context
└── Program.cs       # Application entry point

OrderApp.Tests/        # Test project
├── OrderEndpointsTests.cs    # API integration tests
├── ConcurrentOrderTests.cs   # Concurrent operation tests
└── TestAuthHandler.cs       # JWT test helper
```

## Getting Started

1. Clone the repository:
```bash
git clone https://github.com/estebano/orders_p.git
cd orders_p
```

2. Build the solution:
```bash
dotnet build OrderApp/OrderApp.csproj
```

3. Run the application:
```bash
dotnet run --project OrderApp
```

The API will start at `http://localhost:5000`.

## Configuration

Storage backend can be configured in `appsettings.json`:

```json
{
  "Storage": {
    "Provider": "Sqlite",  # or "InMemory"
    "ConnectionStrings": {
      "Sqlite": "Data Source=orders.db"
    }
  }
}
```

## Running Tests

Run all tests:
```bash
dotnet test OrderApp.Tests/OrderApp.Tests.csproj
```

Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~OrderEndpointsTests"
```

## API Endpoints

All endpoints except `/login` require JWT Bearer authentication.

### Authentication

```http
POST /login
Content-Type: application/json

{
    "username": "demo",
    "password": "password"
}
```

Response:
```json
{
    "token": "eyJhbGciOiJIUzI1..."
}
```

### Orders

#### Get all orders
```http
GET /orders
Authorization: Bearer <token>
```

#### Get order by ID
```http
GET /orders/{id}
Authorization: Bearer <token>
```

#### Create order
```http
POST /orders
Authorization: Bearer <token>
Content-Type: application/json

{
    "description": "New order"
}
```

#### Delete order
```http
DELETE /orders/{id}
Authorization: Bearer <token>
```

## Testing with Postman

1. Import the following collection:

```json
{
    "info": {
        "name": "OrderApp API",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    "item": [
        {
            "name": "Login",
            "request": {
                "method": "POST",
                "url": "http://localhost:5000/login",
                "body": {
                    "mode": "raw",
                    "raw": "{\"username\": \"demo\", \"password\": \"password\"}",
                    "options": { "raw": { "language": "json" } }
                }
            }
        },
        {
            "name": "List Orders",
            "request": {
                "method": "GET",
                "url": "http://localhost:5000/orders",
                "auth": {
                    "type": "bearer",
                    "bearer": ["{{token}}"]
                }
            }
        },
        {
            "name": "Create Order",
            "request": {
                "method": "POST",
                "url": "http://localhost:5000/orders",
                "auth": {
                    "type": "bearer",
                    "bearer": ["{{token}}"]
                },
                "body": {
                    "mode": "raw",
                    "raw": "{\"description\": \"Test order\"}",
                    "options": { "raw": { "language": "json" } }
                }
            }
        },
        {
            "name": "Get Order",
            "request": {
                "method": "GET",
                "url": "http://localhost:5000/orders/{{orderId}}",
                "auth": {
                    "type": "bearer",
                    "bearer": ["{{token}}"]
                }
            }
        },
        {
            "name": "Delete Order",
            "request": {
                "method": "DELETE",
                "url": "http://localhost:5000/orders/{{orderId}}",
                "auth": {
                    "type": "bearer",
                    "bearer": ["{{token}}"]
                }
            }
        }
    ]
}
```

2. Create a Postman environment with variables:
   - `token`: For the JWT token
   - `orderId`: For storing order IDs between requests

3. Execute the requests in sequence:
   1. Call Login and copy the token to the environment variable
   2. Create an order and save its ID
   3. Try getting, listing, and deleting orders

## Development Notes

- The JWT signing key in `appsettings.json` is for development only
- SQLite database is created automatically when using the SQLite provider
- Integration tests use in-memory storage for isolation
- Concurrent operations are handled using ConcurrentDictionary (in-memory) or SQLite's transaction isolation

## Contributing

1. Create a feature branch
2. Make changes
3. Add or update tests
4. Create a pull request

## License

MIT