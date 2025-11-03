using OrderApp.Models;

namespace OrderApp.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(System.Guid id);
    Task<Order> CreateAsync(Order order);
    Task<bool> DeleteAsync(System.Guid id);
    Task<IEnumerable<Order>> ListAsync();
}
