using OrderApp.Models;

namespace OrderApp.Services;

public interface IOrderService
{
    Task<Order> CreateAsync(Order order);
    Task<Order> GetByIdAsync(System.Guid id);
    Task<IEnumerable<Order>> ListAsync();
    Task DeleteAsync(System.Guid id);
    // Process order (updates ShippingStatus under the hood)
    Task ProcessOrder(System.Guid id);
    // Backwards-compatible async naming used by some callers
    // Returns the updated order after processing
    Task<Order> ProcessOrderAsync(System.Guid id);
}
