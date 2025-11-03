using OrderApp.Models;

namespace OrderApp.Services;

public interface IOrderService
{
    Task<Order> CreateAsync(Order order);
    Task<Order> GetByIdAsync(System.Guid id);
    Task<IEnumerable<Order>> ListAsync();
    Task DeleteAsync(System.Guid id);
}
