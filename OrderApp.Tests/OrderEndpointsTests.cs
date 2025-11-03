using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OrderApp.Models;

using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace OrderApp.Tests;

public class OrderEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestAuthHandler _auth;
    private readonly HttpClient _client;

    public OrderEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        
        // Get configuration from the test's appsettings.json
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
            
        _auth = new TestAuthHandler(config);
        _client = _factory.CreateClient();
        
        // Add JWT bearer token to all requests
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _auth.GenerateJwtToken());
    }

    [Fact]
    public async Task GetOrders_Returns200_WhenAuthenticated()
    {
        var response = await _client.GetAsync("/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_Returns401_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient(); // Client without auth header
        var response = await client.GetAsync("/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreatedOrder_WhenValid()
    {
        var order = new Order { Description = "Test Order" };
        var response = await _client.PostAsJsonAsync("/orders", order);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var created = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(order.Description, created.Description);
    }

    [Fact]
    public async Task GetOrderById_ReturnsOrder_WhenExists()
    {
        // Create an order first
        var order = new Order { Description = "Test Get By Id" };
        var createResponse = await _client.PostAsJsonAsync("/orders", order);
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);

        // Get the order by ID
        var response = await _client.GetAsync($"/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var retrieved = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(created.Description, retrieved.Description);
    }

    [Fact]
    public async Task GetOrderById_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrder_Returns204_WhenExists()
    {
        // Create an order first
        var order = new Order { Description = "Test Delete" };
        var createResponse = await _client.PostAsJsonAsync("/orders", order);
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);

        // Delete the order
        var response = await _client.DeleteAsync($"/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteOrder_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync($"/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}