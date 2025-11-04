using OrderApp.Models;
using OrderApp.Repositories;

namespace OrderApp.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo) => _repo = repo;

    public async Task<Order> CreateAsync(Order order)
    {
        if (order.Id == System.Guid.Empty)
            order.Id = System.Guid.NewGuid();

        return await _repo.CreateAsync(order);
    }

    public async Task<Order> GetByIdAsync(System.Guid id)
    {
        var order = await _repo.GetByIdAsync(id);
        if (order is null)
            throw new ArgumentException($"Order with id {id} not found", nameof(id));

        return order;
    }

    public Task<IEnumerable<Order>> ListAsync() => _repo.ListAsync();

    public async Task DeleteAsync(System.Guid id)
    {
        var deleted = await _repo.DeleteAsync(id);
        if (!deleted)
            throw new ArgumentException($"Order with id {id} not found", nameof(id));
    }

    public async Task ProcessOrder(System.Guid id)
    {
        // Delegate to repository which updates ShippingStatus
        await _repo.ProcessOrder(id);
    }

    // Backwards-compatible wrapper that returns the updated order
    public async Task<Order> ProcessOrderAsync(System.Guid id)
    {
        await ProcessOrder(id);
        var order = await _repo.GetByIdAsync(id);
        if (order is null)
            throw new ArgumentException($"Order with id {id} not found", nameof(id));
        return order;
    }
}
