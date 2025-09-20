using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models.Reports;

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

        // Enhanced Organizer Report Methods

        public async Task<SalesReportDto> GetOrganizerSalesReportAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tickets
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .Include(t => t.Customer)
                .Where(t => t.Event.OrganizerId == organizerId && t.IsPaid);

            if (startDate.HasValue)
                query = query.Where(t => t.PurchaseDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.PurchaseDate <= endDate.Value);

            var tickets = await query.ToListAsync();
            var payments = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .Where(p => p.Ticket.Event.OrganizerId == organizerId)
                .Where(p => startDate == null || p.PaymentDate >= startDate)
                .Where(p => endDate == null || p.PaymentDate <= endDate)
                .ToListAsync();

            var totalRevenue = tickets.Sum(t => t.TotalAmount);
            var totalTickets = tickets.Sum(t => t.Quantity);
            var averageTicketPrice = totalTickets > 0 ? totalRevenue / totalTickets : 0;

            // Sales by Event
            var salesByEvent = tickets
                .GroupBy(t => new { t.Event.Id, t.Event.Title, t.Event.Category, t.Event.EventDate })
                .Select(g => new SalesByEvent
                {
                    EventId = g.Key.Id,
                    EventTitle = g.Key.Title,
                    EventCategory = g.Key.Category,
                    EventDate = g.Key.EventDate,
                    OrganizerName = tickets.FirstOrDefault(t => t.Event.Id == g.Key.Id)?.Event.Organizer?.FullName ?? "Unknown",
                    Revenue = g.Sum(t => t.TotalAmount),
                    TicketsSold = g.Sum(t => t.Quantity),
                    Transactions = g.Count()
                })
                .OrderByDescending(s => s.Revenue)
                .ToList();

            // Sales by Category
            var salesByCategory = tickets
                .GroupBy(t => t.Event.Category)
                .Select(g => new SalesByCategory
                {
                    Category = g.Key,
                    Revenue = g.Sum(t => t.TotalAmount),
                    TicketsSold = g.Sum(t => t.Quantity),
                    EventCount = g.Select(t => t.EventId).Distinct().Count(),
                    AverageTicketPrice = g.Sum(t => t.Quantity) > 0 ? g.Sum(t => t.TotalAmount) / g.Sum(t => t.Quantity) : 0
                })
                .ToList();

            // Payment Methods
            var paymentMethods = new PaymentMethodBreakdown();
            foreach (var payment in payments.Where(p => p.Status == "Completed"))
            {
                switch (payment.PaymentMethod.ToLower())
                {
                    case "card":
                        paymentMethods.CardPayments += payment.Amount;
                        paymentMethods.CardTransactions++;
                        break;
                    case "cash":
                        paymentMethods.CashPayments += payment.Amount;
                        paymentMethods.CashTransactions++;
                        break;
                    case "online":
                        paymentMethods.OnlinePayments += payment.Amount;
                        paymentMethods.OnlineTransactions++;
                        break;
                }
            }

            return new SalesReportDto
            {
                TotalRevenue = totalRevenue,
                TotalTicketsSold = totalTickets,
                TotalTransactions = tickets.Count,
                AverageTicketPrice = averageTicketPrice,
                TopEventsBySales = salesByEvent,
                SalesByCategory = salesByCategory,
                PaymentMethods = paymentMethods
            };
        }

        public async Task<Dictionary<string, decimal>> GetOrganizerRevenueByMonthAsync(string organizerId, int year)
        {
            var revenueByMonth = new Dictionary<string, decimal>();

            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var revenue = await _context.Tickets
                    .Include(t => t.Event)
                    .Where(t => t.Event.OrganizerId == organizerId && t.IsPaid &&
                               t.PurchaseDate >= startDate && t.PurchaseDate <= endDate)
                    .SumAsync(t => t.TotalAmount);

                revenueByMonth.Add(startDate.ToString("MMM"), revenue);
            }

            return revenueByMonth;
        }

        public async Task<List<SalesByEvent>> GetOrganizerEventPerformanceAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tickets
                .Include(t => t.Event)
                .ThenInclude(e => e.Organizer)
                .Where(t => t.Event.OrganizerId == organizerId && t.IsPaid);

            if (startDate.HasValue)
                query = query.Where(t => t.PurchaseDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.PurchaseDate <= endDate.Value);

            return await query
                .GroupBy(t => new { t.Event.Id, t.Event.Title, t.Event.Category, t.Event.EventDate, OrganizerName = t.Event.Organizer.FullName })
                .Select(g => new SalesByEvent
                {
                    EventId = g.Key.Id,
                    EventTitle = g.Key.Title,
                    EventCategory = g.Key.Category,
                    EventDate = g.Key.EventDate,
                    OrganizerName = g.Key.OrganizerName ?? "Unknown",
                    Revenue = g.Sum(t => t.TotalAmount),
                    TicketsSold = g.Sum(t => t.Quantity),
                    Transactions = g.Count()
                })
                .OrderByDescending(s => s.Revenue)
                .ToListAsync();
        }

        public async Task<List<SalesByPeriod>> GetOrganizerSalesByPeriodAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null, string periodType = "monthly")
        {
            var query = _context.Tickets
                .Include(t => t.Event)
                .Where(t => t.Event.OrganizerId == organizerId && t.IsPaid);

            if (startDate.HasValue)
                query = query.Where(t => t.PurchaseDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.PurchaseDate <= endDate.Value);

            var tickets = await query.ToListAsync();

            return periodType.ToLower() switch
            {
                "daily" => tickets
                    .GroupBy(t => t.PurchaseDate.Date)
                    .Select(g => new SalesByPeriod
                    {
                        Period = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(t => t.TotalAmount),
                        TicketsSold = g.Sum(t => t.Quantity),
                        Transactions = g.Count()
                    })
                    .OrderBy(s => s.Period)
                    .ToList(),

                "weekly" => tickets
                    .GroupBy(t => GetWeekStart(t.PurchaseDate))
                    .Select(g => new SalesByPeriod
                    {
                        Period = g.Key.ToString("yyyy-MM-dd") + " (Week)",
                        Revenue = g.Sum(t => t.TotalAmount),
                        TicketsSold = g.Sum(t => t.Quantity),
                        Transactions = g.Count()
                    })
                    .OrderBy(s => s.Period)
                    .ToList(),

                _ => tickets // monthly
                    .GroupBy(t => new { t.PurchaseDate.Year, t.PurchaseDate.Month })
                    .Select(g => new SalesByPeriod
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Revenue = g.Sum(t => t.TotalAmount),
                        TicketsSold = g.Sum(t => t.Quantity),
                        Transactions = g.Count()
                    })
                    .OrderBy(s => s.Period)
                    .ToList()
            };
        }

        public async Task<PaymentMethodBreakdown> GetOrganizerPaymentMethodsAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .Where(p => p.Ticket.Event.OrganizerId == organizerId && p.Status == "Completed");

            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);

            var payments = await query.ToListAsync();
            var breakdown = new PaymentMethodBreakdown();

            foreach (var payment in payments)
            {
                switch (payment.PaymentMethod.ToLower())
                {
                    case "card":
                        breakdown.CardPayments += payment.Amount;
                        breakdown.CardTransactions++;
                        break;
                    case "cash":
                        breakdown.CashPayments += payment.Amount;
                        breakdown.CashTransactions++;
                        break;
                    case "online":
                        breakdown.OnlinePayments += payment.Amount;
                        breakdown.OnlineTransactions++;
                        break;
                }
            }

            return breakdown;
        }

        public async Task<List<EventPerformance>> GetOrganizerEventSummaryAsync(string organizerId)
        {
            var events = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Tickets)
                .Include(e => e.Prices)
                .Where(e => e.OrganizerId == organizerId)
                .ToListAsync();

            return events.Select(e => new EventPerformance
            {
                EventId = e.Id,
                Title = e.Title,
                Status = GetEventStatus(e),
                EventDate = e.EventDate,
                TotalCapacity = e.Venue?.Capacity ?? e.Prices.Sum(p => p.Stock),
                TicketsSold = e.Tickets.Where(t => t.IsPaid).Sum(t => t.Quantity),
                Revenue = e.Tickets.Where(t => t.IsPaid).Sum(t => t.TotalAmount),
                SalesPercentage = CalculateSalesPercentage(e)
            }).ToList();
        }

        // Helper methods
        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        private static string GetEventStatus(Event e)
        {
            if (!e.IsPublished) return "Draft";
            if (e.EventDate > DateTime.UtcNow) return "Upcoming";
            if (e.EventDate.Date == DateTime.UtcNow.Date) return "Today";
            return "Past";
        }

        private static decimal CalculateSalesPercentage(Event e)
        {
            var totalCapacity = e.Venue?.Capacity ?? e.Prices.Sum(p => p.Stock);
            var ticketsSold = e.Tickets.Where(t => t.IsPaid).Sum(t => t.Quantity);
            return totalCapacity > 0 ? (decimal)ticketsSold / totalCapacity * 100 : 0;
        }
    }
}
