using System.Collections.Concurrent;
using OrderApp.Models;

namespace OrderApp.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<System.Guid, Order> _store = new();

    public Task<Order?> GetByIdAsync(System.Guid id)
    {
        _store.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<Order> CreateAsync(Order order)
    {
        if (order.Id == System.Guid.Empty)
        {
            order.Id = System.Guid.NewGuid();
        }

        _store[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<bool> DeleteAsync(System.Guid id)
    {
        return Task.FromResult(_store.TryRemove(id, out _));
    }

    public Task<IEnumerable<Order>> ListAsync()
    {
        return Task.FromResult<IEnumerable<Order>>(_store.Values.ToList());
    }

    public async Task ProcessOrder(System.Guid id)
    {
        if (!_store.TryGetValue(id, out var order))
            throw new ArgumentException($"Order with id {id} not found", nameof(id));

        // Atomically mark as processing
        _store.AddOrUpdate(id, order, (k, existing) =>
        {
            existing.ShippingStatus = ShippingStatus.Processing;
            existing.LastUpdatedUtc = DateTime.UtcNow;
            return existing;
        });

        // Yield briefly and simulate work
        await Task.Yield();
        await Task.Delay(10);

        // Mark as shipped
        _store.AddOrUpdate(id, order, (k, existing) =>
        {
            existing.ShippingStatus = ShippingStatus.Shipped;
            existing.LastUpdatedUtc = DateTime.UtcNow;
            return existing;
        });
    }
}
