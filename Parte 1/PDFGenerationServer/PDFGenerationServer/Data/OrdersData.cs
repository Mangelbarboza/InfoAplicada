using Microsoft.EntityFrameworkCore;
using PDFGenerationServer.Models.DB;

namespace PDFGenerationServer.Data
{
    public class OrdersData
    {
        private readonly AppDbContext _context;
        public OrdersData(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalesOrderHeader>> GetOrdersByCustomer(int customerId, DateTime startDate, DateTime endDate)
        {
            return await _context.SalesOrderHeaders
                .Where(o  => o.CustomerId == customerId && o.OrderDate >= startDate 
                && o.OrderDate <= endDate)
                .Include( o => o.SalesOrderDetails)
                .ThenInclude(d => d.SpecialOfferProduct)
                .ThenInclude(sp => sp.Product)
                .OrderBy(o => o.OrderDate) 
                .ToListAsync();
        }
    }
}
