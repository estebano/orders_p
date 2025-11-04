using Microsoft.EntityFrameworkCore;
using OrderApp.Data;
using OrderApp.Models;

namespace OrderApp.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public EfOrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(System.Guid id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        if (order.Id == System.Guid.Empty)
        {
            order.Id = System.Guid.NewGuid();
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteAsync(System.Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Order>> ListAsync()
    {
        return await _context.Orders.OrderByDescending(o => o.CreatedUtc).ToListAsync();
    }

    public async Task ProcessOrder(System.Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            throw new ArgumentException($"Order with id {id} not found", nameof(id));

    // Mark as processing
    order.ShippingStatus = ShippingStatus.Processing;
    order.LastUpdatedUtc = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // Simulate some processing work (short delay)
    await Task.Delay(20);

    // Mark as shipped
    order.ShippingStatus = ShippingStatus.Shipped;
    order.LastUpdatedUtc = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    }
}