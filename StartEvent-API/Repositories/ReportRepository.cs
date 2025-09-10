using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _context;

        public ReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Ticket>> GetOrganizerTicketSalesAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .Where(t => t.Event.OrganizerId == organizerId && t.IsPaid);

            if (startDate.HasValue)
                query = query.Where(t => t.PurchaseDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.PurchaseDate <= endDate.Value);

            return await query.OrderByDescending(t => t.PurchaseDate).ToListAsync();
        }

        public async Task<decimal> GetOrganizerRevenueAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tickets
                .Where(t => t.Event.OrganizerId == organizerId && t.IsPaid);

            if (startDate.HasValue)
                query = query.Where(t => t.PurchaseDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.PurchaseDate <= endDate.Value);

            return await query.SumAsync(t => t.TotalAmount);
        }

        public async Task<IEnumerable<Event>> GetOrganizerEventsAsync(string organizerId)
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Prices)
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetSystemWideTicketSalesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .Where(t => t.IsPaid);

            if (startDate.HasValue)
                query = query.Where(t => t.PurchaseDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.PurchaseDate <= endDate.Value);

            return await query.OrderByDescending(t => t.PurchaseDate).ToListAsync();
        }

        public async Task<decimal> GetSystemWideRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tickets
                .Where(t => t.IsPaid);

            if (startDate.HasValue)
                query = query.Where(t => t.PurchaseDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.PurchaseDate <= endDate.Value);

            return await query.SumAsync(t => t.TotalAmount);
        }

        public async Task<IEnumerable<ApplicationUser>> GetSystemUsersAsync()
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetAllEventsWithMetricsAsync()
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.Prices)
                .Include(e => e.Tickets)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var organizers = await _context.Users
                .Where(u => _context.Events.Any(e => e.OrganizerId == u.Id))
                .CountAsync();

            return new Dictionary<string, int>
            {
                { "TotalUsers", totalUsers },
                { "ActiveUsers", activeUsers },
                { "Organizers", organizers },
                { "Customers", totalUsers - organizers }
            };
        }

        public async Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year)
        {
            var revenueByMonth = new Dictionary<string, decimal>();
            
            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                
                var revenue = await _context.Tickets
                    .Where(t => t.IsPaid && t.PurchaseDate >= startDate && t.PurchaseDate <= endDate)
                    .SumAsync(t => t.TotalAmount);
                
                revenueByMonth.Add(startDate.ToString("MMM"), revenue);
            }
            
            return revenueByMonth;
        }

        public async Task<Dictionary<string, int>> GetTicketSalesByCategoryAsync()
        {
            return await _context.Tickets
                .Include(t => t.Event)
                .Where(t => t.IsPaid)
                .GroupBy(t => t.Event.Category)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(t => t.Quantity));
        }

        public async Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count = 10)
        {
            return await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Ticket)
                .OrderByDescending(p => p.PaymentDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetTotalActiveUsersAsync()
        {
            return await _context.Users.CountAsync(u => u.IsActive);
        }

        public async Task<int> GetTotalEventsAsync()
        {
            return await _context.Events.CountAsync();
        }

        public async Task<int> GetTotalTicketsSoldAsync()
        {
            return await _context.Tickets
                .Where(t => t.IsPaid)
                .SumAsync(t => t.Quantity);
        }
    }
}
