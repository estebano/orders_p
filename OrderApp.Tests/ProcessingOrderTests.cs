using System;
using System.Threading.Tasks;
using OrderApp.Models;
using OrderApp.Repositories;
using OrderApp.Services;
using Xunit;

namespace OrderApp.Tests
{
    public class ProcessingOrderTests
    {
        [Fact]
        public async Task MultiThreadedProcessing_ProcessesOrdersConcurrently()
        {
            // Arrange - use in-memory repository for isolation
            var repo = new InMemoryOrderRepository();
            var service = new OrderService(repo);

            var o1 = await repo.CreateAsync(new Order { Description = "P1" });
            var o2 = await repo.CreateAsync(new Order { Description = "P2" });
            var invalidId = Guid.NewGuid(); // not present in store

            // Act - simulate multiple threads processing orders
            Task[] tasks = new Task[3];
            tasks[0] = Task.Run(() => service.ProcessOrder(o1.Id));
            tasks[1] = Task.Run(() => service.ProcessOrder(o2.Id));
            tasks[2] = Task.Run(async () =>
            {
                // invalid id should throw
                await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessOrder(invalidId));
            });

            await Task.WhenAll(tasks);

            // Assert - processed orders should be marked as Shipped
            var updated1 = await repo.GetByIdAsync(o1.Id);
            var updated2 = await repo.GetByIdAsync(o2.Id);

            Assert.NotNull(updated1);
            Assert.NotNull(updated2);
            Assert.Equal(ShippingStatus.Shipped, updated1!.ShippingStatus);
            Assert.Equal(ShippingStatus.Shipped, updated2!.ShippingStatus);
        }
    }
}
