using Microsoft.EntityFrameworkCore;
using OrdersApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace OrdersApi.Persistence
{
    public class OrderRepository : IOrderRepository
    {
        private OrdersContext _context;

        public OrderRepository(OrdersContext context)
        {
            _context = context;
        }
        public Order GetOrder(Guid id)
        {
            return _context.Orders.
                Include("OrderDetails").
                FirstOrDefault(c => c.OrderId == id);

        }

        public async Task<Order> GetOrderAsync(Guid id)
        {
            return await _context.Orders.
                Include("OrderDetails").
                FirstOrDefaultAsync(c => c.OrderId == id);
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        public Task RegisterOrder(Order order)
        {
            _context.Add(order);
            _context.SaveChanges();
            return Task.FromResult(true);
        }

        public void UpdateOrder(Order order)
        {
            _context.Entry(order).State = EntityState.Modified;
            _context.SaveChanges();
        }
    }
}
