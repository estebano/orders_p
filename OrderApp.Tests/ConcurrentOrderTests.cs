using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using OrderApp.Models;
using Xunit;

namespace OrderApp.Tests;

public class ConcurrentOrderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly TestAuthHandler _auth;
    private readonly HttpClient _client;

    public ConcurrentOrderTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        _auth = new TestAuthHandler(config);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _auth.GenerateJwtToken());
    }

    [Fact]
    public async Task ParallelOrderCreation_ShouldCreateUniqueOrders()
    {
        // Arrange
        const int numOrders = 50;
        var tasks = new List<Task<Order>>();
        var descriptions = Enumerable.Range(1, numOrders)
            .Select(i => $"Concurrent Order {i}")
            .ToList();

        // Act - Create orders in parallel
        foreach (var desc in descriptions)
        {
            tasks.Add(CreateOrderAsync(new Order { Description = desc }));
        }

        var orders = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(numOrders, orders.Length);
        Assert.Equal(numOrders, orders.Select(o => o.Id).Distinct().Count());
        Assert.All(orders, o => Assert.NotEqual(Guid.Empty, o.Id));
    }

    [Fact]
    public async Task ParallelOrderDeletion_ShouldHandleConcurrentDeletes()
    {
        // Arrange - Create orders sequentially first
        var orders = new List<Order>();
        for (int i = 0; i < 10; i++)
        {
            var order = await CreateOrderAsync(new Order { Description = $"Delete Test {i}" });
            orders.Add(order);
        }

        // Act - Delete in parallel
        var deleteTasks = orders.Select(o => DeleteOrderAsync(o.Id)).ToList();
        await Task.WhenAll(deleteTasks);

        // Verify - Try to get each order
        var getTasks = orders.Select(o => GetOrderAsync(o.Id)).ToList();
        var results = await Task.WhenAll(getTasks);

        // Assert - All should be null (not found)
        Assert.All(results, result => Assert.Null(result));
    }

    private async Task<Order> CreateOrderAsync(Order order)
    {
        var response = await _client.PostAsJsonAsync("/orders", order);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    private async Task<bool> DeleteOrderAsync(Guid id)
    {
        var response = await _client.DeleteAsync($"/orders/{id}");
        return response.IsSuccessStatusCode;
    }

    private async Task<Order?> GetOrderAsync(Guid id)
    {
        var response = await _client.GetAsync($"/orders/{id}");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    private async Task<Order?> ProcessOrderAsync(Guid id)
    {
        var response = await _client.PostAsync($"/orders/{id}/process", null);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    [Fact]
    public async Task ParallelOrderProcessing_ShouldHandleConcurrentProcessing()
    {
        // Arrange - Create an order
        var order = await CreateOrderAsync(new Order { Description = "Process Test" });

        // Act - Process the same order multiple times in parallel
        var processTasks = Enumerable.Range(0, 5)
            .Select(_ => ProcessOrderAsync(order.Id))
            .ToList();

        var results = await Task.WhenAll(processTasks);

        // Assert - All processing attempts should return an order
        Assert.All(results, result => Assert.NotNull(result));

        // Get the final state
        var finalOrder = await GetOrderAsync(order.Id);
        Assert.NotNull(finalOrder);

        // The order should have progressed through the states
        Assert.True(finalOrder.ShippingStatus == ShippingStatus.Shipped || finalOrder.ShippingStatus == ShippingStatus.Processing);
        // All timestamps should be valid
        Assert.True(finalOrder.LastUpdatedUtc > finalOrder.CreatedUtc);
    }
}