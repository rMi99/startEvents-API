using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models.Reports;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Generates comprehensive sales report
        /// </summary>
        /// <param name="startDate">Start date for the report period</param>
        /// <param name="endDate">End date for the report period</param>
        /// <param name="groupBy">Period grouping: daily, weekly, monthly, yearly</param>
        /// <returns>Sales report with revenue, transactions, and breakdowns</returns>
        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string groupBy = "monthly")
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var paymentsQuery = _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .ThenInclude(e => e.Organizer)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate);

            var payments = await paymentsQuery.ToListAsync();
            var completedPayments = payments.Where(p => p.Status == "Completed").ToList();

            // Calculate totals
            var totalRevenue = completedPayments.Sum(p => p.Amount);
            var totalTicketsSold = completedPayments.Count;
            var totalTransactions = completedPayments.Count;
            var averageTicketPrice = totalTicketsSold > 0 ? totalRevenue / totalTicketsSold : 0;

            // Sales by period
            var salesByPeriod = GetSalesByPeriod(completedPayments, groupBy);

            // Top events by sales
            var topEventsBySales = completedPayments
                .GroupBy(p => new { p.Ticket.Event.Id, p.Ticket.Event.Title, p.Ticket.Event.Category, p.Ticket.Event.EventDate, p.Ticket.Event.Organizer.FullName })
                .Select(g => new SalesByEvent
                {
                    EventId = g.Key.Id,
                    EventTitle = g.Key.Title,
                    EventCategory = g.Key.Category,
                    EventDate = g.Key.EventDate,
                    OrganizerName = g.Key.FullName ?? "Unknown",
                    Revenue = g.Sum(p => p.Amount),
                    TicketsSold = g.Count(),
                    Transactions = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            // Sales by category
            var salesByCategory = completedPayments
                .GroupBy(p => p.Ticket.Event.Category)
                .Select(g => new SalesByCategory
                {
                    Category = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    TicketsSold = g.Count(),
                    EventCount = g.Select(p => p.Ticket.EventId).Distinct().Count(),
                    AverageTicketPrice = g.Average(p => p.Amount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Sales by organizer
            var salesByOrganizer = completedPayments
                .GroupBy(p => new { p.Ticket.Event.OrganizerId, p.Ticket.Event.Organizer.FullName, p.Ticket.Event.Organizer.OrganizationName })
                .Select(g => new SalesByOrganizer
                {
                    OrganizerId = g.Key.OrganizerId,
                    OrganizerName = g.Key.FullName ?? "Unknown",
                    OrganizationName = g.Key.OrganizationName,
                    Revenue = g.Sum(p => p.Amount),
                    TicketsSold = g.Count(),
                    EventCount = g.Select(p => p.Ticket.EventId).Distinct().Count(),
                    AverageRevenuePerEvent = g.Select(p => p.Ticket.EventId).Distinct().Count() > 0 ? g.Sum(p => p.Amount) / g.Select(p => p.Ticket.EventId).Distinct().Count() : 0
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            // Payment method breakdown
            var paymentMethods = new PaymentMethodBreakdown
            {
                CardPayments = completedPayments.Where(p => p.PaymentMethod == "Card").Sum(p => p.Amount),
                CashPayments = completedPayments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount),
                OnlinePayments = completedPayments.Where(p => p.PaymentMethod == "Online").Sum(p => p.Amount),
                CardTransactions = completedPayments.Count(p => p.PaymentMethod == "Card"),
                CashTransactions = completedPayments.Count(p => p.PaymentMethod == "Cash"),
                OnlineTransactions = completedPayments.Count(p => p.PaymentMethod == "Online")
            };

            var salesReport = new SalesReportDto
            {
                TotalRevenue = totalRevenue,
                TotalTicketsSold = totalTicketsSold,
                TotalTransactions = totalTransactions,
                AverageTicketPrice = averageTicketPrice,
                SalesByPeriod = salesByPeriod,
                TopEventsBySales = topEventsBySales,
                SalesByCategory = salesByCategory,
                SalesByOrganizer = salesByOrganizer,
                PaymentMethods = paymentMethods
            };

            return Ok(salesReport);
        }

        /// <summary>
        /// Generates comprehensive user report
        /// </summary>
        /// <param name="startDate">Start date for registration trends</param>
        /// <param name="endDate">End date for registration trends</param>
        /// <returns>User statistics and trends</returns>
        [HttpGet("users")]
        public async Task<IActionResult> GetUserReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var users = await _context.Users.ToListAsync();
            var roles = await _context.Roles.ToListAsync();
            var userRoles = await _context.UserRoles.ToListAsync();

            // Get role counts
            var organizerRoleId = roles.FirstOrDefault(r => r.Name == "Organizer")?.Id;
            var customerRoleId = roles.FirstOrDefault(r => r.Name == "Customer")?.Id;
            var adminRoleId = roles.FirstOrDefault(r => r.Name == "Admin")?.Id;

            var organizers = organizerRoleId != null ? userRoles.Count(ur => ur.RoleId == organizerRoleId) : 0;
            var customers = customerRoleId != null ? userRoles.Count(ur => ur.RoleId == customerRoleId) : 0;
            var admins = adminRoleId != null ? userRoles.Count(ur => ur.RoleId == adminRoleId) : 0;

            // Registration trend
            var registrationTrend = GetUserRegistrationTrend(users, userRoles, organizerRoleId, customerRoleId, startDate.Value, endDate.Value);

            // Most active users (based on ticket purchases and events organized)
            var tickets = await _context.Tickets.Include(t => t.Customer).Where(t => t.IsPaid).ToListAsync();
            var events = await _context.Events.Include(e => e.Organizer).ToListAsync();

            var mostActiveUsers = users
                .Select(u => new UserActivitySummary
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Role = GetUserRole(u.Id, userRoles, roles),
                    LastLogin = u.LastLogin ?? u.CreatedAt,
                    TicketsPurchased = tickets.Count(t => t.CustomerId == u.Id),
                    TotalSpent = tickets.Where(t => t.CustomerId == u.Id).Sum(t => t.TotalAmount),
                    EventsOrganized = events.Count(e => e.OrganizerId == u.Id)
                })
                .Where(u => u.TicketsPurchased > 0 || u.EventsOrganized > 0)
                .OrderByDescending(u => u.TicketsPurchased + (u.EventsOrganized * 10)) // Weight events more
                .Take(10)
                .ToList();

            // Top organizers
            var topOrganizers = users
                .Where(u => organizerRoleId != null && userRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == organizerRoleId))
                .Select(u => new OrganizerPerformance
                {
                    OrganizerId = u.Id,
                    OrganizerName = u.FullName,
                    EventsCreated = events.Count(e => e.OrganizerId == u.Id),
                    PublishedEvents = events.Count(e => e.OrganizerId == u.Id && e.IsPublished),
                    TotalRevenue = tickets.Where(t => events.Any(e => e.Id == t.EventId && e.OrganizerId == u.Id)).Sum(t => t.TotalAmount),
                    TicketsSold = tickets.Count(t => events.Any(e => e.Id == t.EventId && e.OrganizerId == u.Id)),
                    AverageEventRating = 0 // Placeholder - implement if you have ratings
                })
                .OrderByDescending(o => o.TotalRevenue)
                .Take(10)
                .ToList();

            var userReport = new UserReportDto
            {
                TotalUsers = users.Count,
                ActiveUsers = users.Count(u => u.IsActive),
                InactiveUsers = users.Count(u => !u.IsActive),
                VerifiedUsers = users.Count(u => u.EmailConfirmed),
                UnverifiedUsers = users.Count(u => !u.EmailConfirmed),
                Organizers = organizers,
                Customers = customers,
                Admins = admins,
                RegistrationTrend = registrationTrend,
                MostActiveUsers = mostActiveUsers,
                TopOrganizers = topOrganizers
            };

            return Ok(userReport);
        }

        /// <summary>
        /// Generates comprehensive events report
        /// </summary>
        /// <param name="startDate">Start date for event analysis</param>
        /// <param name="endDate">End date for event analysis</param>
        /// <returns>Event statistics and performance metrics</returns>
        [HttpGet("events")]
        public async Task<IActionResult> GetEventReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var events = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .Include(e => e.Tickets)
                .Include(e => e.Prices)
                .ToListAsync();

            var filteredEvents = events.Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate).ToList();
            var now = DateTime.UtcNow;

            // Basic counts
            var totalEvents = events.Count;
            var publishedEvents = events.Count(e => e.IsPublished);
            var unpublishedEvents = events.Count(e => !e.IsPublished);
            var upcomingEvents = events.Count(e => e.EventDate >= now);
            var pastEvents = events.Count(e => e.EventDate < now);
            var ongoingEvents = events.Count(e => e.EventDate.Date == now.Date);

            // Events by category
            var eventsByCategory = events
                .GroupBy(e => e.Category)
                .Select(g => new EventsByCategory
                {
                    Category = g.Key,
                    EventCount = g.Count(),
                    PublishedCount = g.Count(e => e.IsPublished),
                    AverageTicketsSold = (decimal)g.Average(e => e.Tickets?.Count(t => t.IsPaid) ?? 0),
                    AverageRevenue = (decimal)g.Average(e => e.Tickets?.Where(t => t.IsPaid).Sum(t => t.TotalAmount) ?? 0)
                })
                .OrderByDescending(x => x.EventCount)
                .ToList();

            // Events by period
            var eventsByPeriod = GetEventsByPeriod(filteredEvents, "monthly");

            // Most popular events (by tickets sold)
            var mostPopularEvents = events
                .Where(e => e.Tickets?.Any(t => t.IsPaid) == true)
                .Select(e => new PopularEvent
                {
                    EventId = e.Id,
                    Title = e.Title,
                    Category = e.Category,
                    EventDate = e.EventDate,
                    OrganizerName = e.Organizer?.FullName ?? "Unknown",
                    TicketsSold = e.Tickets?.Count(t => t.IsPaid) ?? 0,
                    Revenue = e.Tickets?.Where(t => t.IsPaid).Sum(t => t.TotalAmount) ?? 0,
                    Views = 0 // Placeholder - implement if you track views
                })
                .OrderByDescending(x => x.TicketsSold)
                .Take(10)
                .ToList();

            // Event performance
            var eventPerformance = events
                .Where(e => e.IsPublished)
                .Select(e => new EventPerformance
                {
                    EventId = e.Id,
                    Title = e.Title,
                    Status = e.EventDate < now ? "Past" : e.IsPublished ? "Published" : "Draft",
                    EventDate = e.EventDate,
                    TotalCapacity = e.Prices?.Sum(p => p.Stock) ?? 0,
                    TicketsSold = e.Tickets?.Count(t => t.IsPaid) ?? 0,
                    Revenue = e.Tickets?.Where(t => t.IsPaid).Sum(t => t.TotalAmount) ?? 0
                })
                .ToList();

            // Calculate sales percentage for each event
            eventPerformance.ForEach(ep =>
            {
                ep.SalesPercentage = ep.TotalCapacity > 0 ? (decimal)ep.TicketsSold / ep.TotalCapacity * 100 : 0;
            });

            // Venue utilization
            var venues = await _context.Venues.Include(v => v.Events).ToListAsync();
            var venueStats = new VenueUtilization
            {
                TotalVenues = venues.Count,
                ActiveVenues = venues.Count(v => v.Events?.Any() == true),
                MostUsedVenues = venues
                    .Where(v => v.Events?.Any() == true)
                    .Select(v => new VenueUsage
                    {
                        VenueId = v.Id,
                        VenueName = v.Name,
                        Location = v.Location,
                        EventCount = v.Events?.Count ?? 0,
                        UpcomingEventCount = v.Events?.Count(e => e.EventDate >= now) ?? 0,
                        UtilizationRate = v.Events?.Count ?? 0 // Simplified calculation
                    })
                    .OrderByDescending(x => x.EventCount)
                    .Take(5)
                    .ToList(),
                LeastUsedVenues = venues
                    .Select(v => new VenueUsage
                    {
                        VenueId = v.Id,
                        VenueName = v.Name,
                        Location = v.Location,
                        EventCount = v.Events?.Count ?? 0,
                        UpcomingEventCount = v.Events?.Count(e => e.EventDate >= now) ?? 0,
                        UtilizationRate = v.Events?.Count ?? 0
                    })
                    .OrderBy(x => x.EventCount)
                    .Take(5)
                    .ToList()
            };

            var eventReport = new EventReportDto
            {
                TotalEvents = totalEvents,
                PublishedEvents = publishedEvents,
                UnpublishedEvents = unpublishedEvents,
                UpcomingEvents = upcomingEvents,
                PastEvents = pastEvents,
                OngoingEvents = ongoingEvents,
                EventsByCategory = eventsByCategory,
                EventsByPeriod = eventsByPeriod,
                MostPopularEvents = mostPopularEvents,
                EventPerformance = eventPerformance.OrderByDescending(x => x.Revenue).Take(20).ToList(),
                VenueStats = venueStats
            };

            return Ok(eventReport);
        }

        /// <summary>
        /// Generates comprehensive revenue report
        /// </summary>
        /// <param name="startDate">Start date for revenue analysis</param>
        /// <param name="endDate">End date for revenue analysis</param>
        /// <param name="groupBy">Period grouping for revenue trends</param>
        /// <returns>Revenue analysis with projections</returns>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string groupBy = "monthly")
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var payments = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .ThenInclude(e => e.Organizer)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToListAsync();

            var totalRevenue = payments.Sum(p => p.Amount);
            var completedRevenue = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount);
            var pendingRevenue = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount);
            var refundedAmount = payments.Where(p => p.Status == "Refunded").Sum(p => p.Amount);
            var netRevenue = completedRevenue - refundedAmount;

            // Revenue by period with growth calculation
            var revenueByPeriod = GetRevenueByPeriod(payments, groupBy);

            // Top revenue events
            var topRevenueEvents = payments
                .Where(p => p.Status == "Completed")
                .GroupBy(p => new { p.Ticket.Event.Id, p.Ticket.Event.Title, p.Ticket.Event.EventDate, p.Ticket.Event.Organizer.FullName })
                .Select(g => new RevenueByEvent
                {
                    EventId = g.Key.Id,
                    EventTitle = g.Key.Title,
                    EventDate = g.Key.EventDate,
                    OrganizerName = g.Key.FullName ?? "Unknown",
                    Revenue = g.Sum(p => p.Amount),
                    PendingAmount = 0, // Would need to calculate separately
                    CompletedAmount = g.Sum(p => p.Amount),
                    TicketsSold = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            // Revenue by organizer
            var revenueByOrganizer = payments
                .Where(p => p.Status == "Completed")
                .GroupBy(p => new { p.Ticket.Event.OrganizerId, p.Ticket.Event.Organizer.FullName })
                .Select(g => new RevenueByOrganizer
                {
                    OrganizerId = g.Key.OrganizerId,
                    OrganizerName = g.Key.FullName ?? "Unknown",
                    TotalRevenue = g.Sum(p => p.Amount),
                    PendingRevenue = 0, // Placeholder
                    CompletedRevenue = g.Sum(p => p.Amount),
                    EventCount = g.Select(p => p.Ticket.Event.Id).Distinct().Count(),
                    AverageRevenuePerEvent = g.Select(p => p.Ticket.Event.Id).Distinct().Count() > 0 ? g.Sum(p => p.Amount) / g.Select(p => p.Ticket.Event.Id).Distinct().Count() : 0
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            // Revenue by category
            var revenueByCategory = payments
                .Where(p => p.Status == "Completed")
                .GroupBy(p => p.Ticket.Event.Category)
                .Select(g => new RevenueByCategory
                {
                    Category = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    PendingRevenue = 0, // Placeholder
                    CompletedRevenue = g.Sum(p => p.Amount),
                    EventCount = g.Select(p => p.Ticket.Event.Id).Distinct().Count(),
                    MarketShare = completedRevenue > 0 ? (g.Sum(p => p.Amount) / completedRevenue) * 100 : 0
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Simple revenue projection based on recent trends
            var last3MonthsRevenue = payments
                .Where(p => p.Status == "Completed" && p.PaymentDate >= DateTime.UtcNow.AddMonths(-3))
                .Sum(p => p.Amount);
            var monthlyAverage = last3MonthsRevenue / 3;
            var growthRate = CalculateGrowthRate(revenueByPeriod);

            var projection = new RevenueProjection
            {
                ProjectedMonthlyRevenue = monthlyAverage * (1 + growthRate / 100),
                ProjectedQuarterlyRevenue = monthlyAverage * 3 * (1 + growthRate / 100),
                ProjectedAnnualRevenue = monthlyAverage * 12 * (1 + growthRate / 100),
                GrowthRate = growthRate,
                ProjectionBasis = "Last 3 months average with trend analysis"
            };

            var revenueReport = new RevenueReportDto
            {
                TotalRevenue = totalRevenue,
                PendingRevenue = pendingRevenue,
                CompletedRevenue = completedRevenue,
                RefundedAmount = refundedAmount,
                NetRevenue = netRevenue,
                RevenueByPeriod = revenueByPeriod,
                TopRevenueEvents = topRevenueEvents,
                RevenueByOrganizer = revenueByOrganizer,
                RevenueByCategory = revenueByCategory,
                Projection = projection
            };

            return Ok(revenueReport);
        }

        /// <summary>
        /// Gets dashboard summary with key metrics from all report types
        /// </summary>
        /// <returns>Combined dashboard metrics</returns>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            // Get basic counts
            var totalUsers = await _context.Users.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var publishedEvents = await _context.Events.CountAsync(e => e.IsPublished);
            var upcomingEvents = await _context.Events.CountAsync(e => e.EventDate >= now);

            // Revenue metrics
            var completedPayments = await _context.Payments
                .Where(p => p.Status == "Completed")
                .ToListAsync();

            var totalRevenue = completedPayments.Sum(p => p.Amount);
            var monthlyRevenue = completedPayments
                .Where(p => p.PaymentDate >= startOfMonth)
                .Sum(p => p.Amount);
            var yearlyRevenue = completedPayments
                .Where(p => p.PaymentDate >= startOfYear)
                .Sum(p => p.Amount);

            // Ticket sales
            var totalTicketsSold = await _context.Tickets.CountAsync(t => t.IsPaid);
            var monthlyTicketsSold = await _context.Tickets
                .CountAsync(t => t.IsPaid && t.PurchaseDate >= startOfMonth);

            // Growth calculations (compare with previous month)
            var previousMonthStart = startOfMonth.AddMonths(-1);
            var previousMonthEnd = startOfMonth.AddDays(-1);

            var previousMonthRevenue = completedPayments
                .Where(p => p.PaymentDate >= previousMonthStart && p.PaymentDate <= previousMonthEnd)
                .Sum(p => p.Amount);

            var previousMonthTickets = await _context.Tickets
                .CountAsync(t => t.IsPaid && t.PurchaseDate >= previousMonthStart && t.PurchaseDate <= previousMonthEnd);

            var revenueGrowth = previousMonthRevenue > 0
                ? ((monthlyRevenue - previousMonthRevenue) / previousMonthRevenue) * 100
                : 0;

            var ticketGrowth = previousMonthTickets > 0
                ? ((decimal)(monthlyTicketsSold - previousMonthTickets) / previousMonthTickets) * 100
                : 0;

            // Top performing categories this month
            var topCategories = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .Where(p => p.Status == "Completed" && p.PaymentDate >= startOfMonth)
                .GroupBy(p => p.Ticket.Event.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    TicketsSold = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            // Recent activity
            var recentEvents = await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.CreatedAt >= now.AddDays(-7))
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Category,
                    OrganizerName = e.Organizer.FullName,
                    e.CreatedAt
                })
                .ToListAsync();

            var dashboardSummary = new
            {
                // Key Metrics
                TotalUsers = totalUsers,
                TotalEvents = totalEvents,
                PublishedEvents = publishedEvents,
                UpcomingEvents = upcomingEvents,

                // Revenue Metrics
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                YearlyRevenue = yearlyRevenue,
                RevenueGrowth = revenueGrowth,

                // Sales Metrics
                TotalTicketsSold = totalTicketsSold,
                MonthlyTicketsSold = monthlyTicketsSold,
                TicketGrowth = ticketGrowth,
                AverageTicketPrice = totalTicketsSold > 0 ? totalRevenue / totalTicketsSold : 0,

                // Performance Data
                TopCategories = topCategories,
                RecentEvents = recentEvents,

                // Quick Stats
                EventsThisMonth = await _context.Events.CountAsync(e => e.CreatedAt >= startOfMonth),
                NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= startOfMonth),
                ActiveOrganizers = await _context.Events
                    .Where(e => e.CreatedAt >= startOfMonth)
                    .Select(e => e.OrganizerId)
                    .Distinct()
                    .CountAsync(),

                // System Health
                LastUpdated = now
            };

            return Ok(dashboardSummary);
        }

        /// <summary>
        /// Exports sales report as CSV
        /// </summary>
        /// <param name="startDate">Start date for the report</param>
        /// <param name="endDate">End date for the report</param>
        /// <returns>CSV file download</returns>
        [HttpGet("sales/export/csv")]
        public async Task<IActionResult> ExportSalesReportCsv(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var payments = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .ThenInclude(e => e.Organizer)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var csvContent = "Date,Event Title,Category,Organizer,Payment Method,Amount,Transaction ID\n";

            foreach (var payment in payments)
            {
                csvContent += $"{payment.PaymentDate:yyyy-MM-dd HH:mm:ss}," +
                            $"\"{payment.Ticket.Event.Title}\"," +
                            $"{payment.Ticket.Event.Category}," +
                            $"\"{payment.Ticket.Event.Organizer?.FullName ?? "Unknown"}\"," +
                            $"{payment.PaymentMethod}," +
                            $"{payment.Amount}," +
                            $"{payment.TransactionId ?? ""}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"sales-report-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.csv");
        }

        /// <summary>
        /// Exports user report as CSV
        /// </summary>
        /// <returns>CSV file download</returns>
        [HttpGet("users/export/csv")]
        public async Task<IActionResult> ExportUserReportCsv()
        {
            var users = await _context.Users.ToListAsync();
            var tickets = await _context.Tickets.Where(t => t.IsPaid).ToListAsync();
            var events = await _context.Events.ToListAsync();

            var csvContent = "User ID,Full Name,Email,Role,Registration Date,Status,Events Organized,Tickets Purchased,Total Spent,Last Login\n";

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var role = userRoles.FirstOrDefault() ?? "Customer";
                var eventsOrganized = events.Count(e => e.OrganizerId == user.Id);
                var ticketsPurchased = tickets.Count(t => t.CustomerId == user.Id);
                var totalSpent = tickets.Where(t => t.CustomerId == user.Id).Sum(t => t.TotalAmount);

                csvContent += $"{user.Id}," +
                            $"\"{user.FullName}\"," +
                            $"{user.Email}," +
                            $"{role}," +
                            $"{user.CreatedAt:yyyy-MM-dd}," +
                            $"{(user.IsActive ? "Active" : "Inactive")}," +
                            $"{eventsOrganized}," +
                            $"{ticketsPurchased}," +
                            $"{totalSpent}," +
                            $"{user.LastLogin?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"user-report-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }

        /// <summary>
        /// Exports event report as CSV
        /// </summary>
        /// <param name="startDate">Start date for events</param>
        /// <param name="endDate">End date for events</param>
        /// <returns>CSV file download</returns>
        [HttpGet("events/export/csv")]
        public async Task<IActionResult> ExportEventReportCsv(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            var events = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .Include(e => e.Tickets)
                .Include(e => e.Prices)
                .Where(e => e.EventDate >= startDate && e.EventDate <= endDate)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var csvContent = "Event ID,Title,Category,Organizer,Venue,Event Date,Status,Tickets Sold,Total Capacity,Revenue,Created Date\n";

            foreach (var eventItem in events)
            {
                var ticketsSold = eventItem.Tickets?.Count(t => t.IsPaid) ?? 0;
                var totalCapacity = eventItem.Prices?.Sum(p => p.Stock) ?? 0;
                var revenue = eventItem.Tickets?.Where(t => t.IsPaid).Sum(t => t.TotalAmount) ?? 0;

                csvContent += $"{eventItem.Id}," +
                            $"\"{eventItem.Title}\"," +
                            $"{eventItem.Category}," +
                            $"\"{eventItem.Organizer?.FullName ?? "Unknown"}\"," +
                            $"\"{eventItem.Venue?.Name ?? "Unknown"}\"," +
                            $"{eventItem.EventDate:yyyy-MM-dd HH:mm:ss}," +
                            $"{(eventItem.IsPublished ? "Published" : "Draft")}," +
                            $"{ticketsSold}," +
                            $"{totalCapacity}," +
                            $"{revenue}," +
                            $"{eventItem.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"event-report-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.csv");
        }

        /// <summary>
        /// Exports all reports as a comprehensive JSON export
        /// </summary>
        /// <param name="startDate">Start date for the reports</param>
        /// <param name="endDate">End date for the reports</param>
        /// <returns>JSON file download</returns>
        [HttpGet("export/json")]
        public async Task<IActionResult> ExportAllReportsJson(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-12);
            endDate ??= DateTime.UtcNow;

            // Get all report data
            var salesReport = await GetSalesReportData(startDate.Value, endDate.Value);
            var userReport = await GetUserReportData();
            var eventReport = await GetEventReportData(startDate.Value, endDate.Value);
            var revenueReport = await GetRevenueReportData(startDate.Value, endDate.Value);
            var dashboardSummary = await GetDashboardSummaryData();

            var comprehensiveReport = new
            {
                ExportDate = DateTime.UtcNow,
                DateRange = new { StartDate = startDate, EndDate = endDate },
                SalesReport = salesReport,
                UserReport = userReport,
                EventReport = eventReport,
                RevenueReport = revenueReport,
                DashboardSummary = dashboardSummary
            };

            var json = System.Text.Json.JsonSerializer.Serialize(comprehensiveReport, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"comprehensive-report-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.json");
        }

        /// <summary>
        /// Exports dashboard summary as Excel-compatible CSV
        /// </summary>
        /// <returns>CSV file formatted for Excel</returns>
        [HttpGet("dashboard/export/excel")]
        public async Task<IActionResult> ExportDashboardExcel()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var totalTicketsSold = await _context.Tickets.CountAsync(t => t.IsPaid);
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= thirtyDaysAgo);
            var newEvents = await _context.Events.CountAsync(e => e.CreatedAt >= thirtyDaysAgo);
            var recentSales = await _context.Tickets.CountAsync(t => t.IsPaid && t.PurchaseDate >= thirtyDaysAgo);
            var recentRevenue = await _context.Payments
                .Where(p => p.Status == "Completed" && p.PaymentDate >= thirtyDaysAgo)
                .SumAsync(p => p.Amount);

            var topEvents = await _context.Events
                .Include(e => e.Tickets)
                .Where(e => e.Tickets.Any(t => t.IsPaid))
                .Select(e => new
                {
                    e.Title,
                    TicketsSold = e.Tickets.Count(t => t.IsPaid),
                    Revenue = e.Tickets.Where(t => t.IsPaid).Sum(t => t.TotalAmount)
                })
                .OrderByDescending(e => e.Revenue)
                .Take(5)
                .ToListAsync();

            var categoryRevenue = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .Where(p => p.Status == "Completed")
                .GroupBy(p => p.Ticket.Event.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    Percentage = totalRevenue > 0 ? (double)(g.Sum(p => p.Amount) / totalRevenue) * 100 : 0
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var csvContent = "StartEvent API Dashboard Summary Report\n";
            csvContent += $"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n";

            // Overview Section
            csvContent += "OVERVIEW METRICS\n";
            csvContent += "Metric,Value\n";
            csvContent += $"Total Users,{totalUsers}\n";
            csvContent += $"Total Events,{totalEvents}\n";
            csvContent += $"Total Tickets Sold,{totalTicketsSold}\n";
            csvContent += $"Total Revenue,${totalRevenue:F2}\n\n";

            // Recent Activity
            csvContent += "RECENT ACTIVITY (Last 30 Days)\n";
            csvContent += "Metric,Value\n";
            csvContent += $"New Users,{newUsers}\n";
            csvContent += $"New Events,{newEvents}\n";
            csvContent += $"Recent Sales,{recentSales}\n";
            csvContent += $"Recent Revenue,${recentRevenue:F2}\n\n";

            // Top Events
            csvContent += "TOP PERFORMING EVENTS\n";
            csvContent += "Event Title,Tickets Sold,Revenue\n";
            foreach (var topEvent in topEvents)
            {
                csvContent += $"\"{topEvent.Title}\",{topEvent.TicketsSold},${topEvent.Revenue:F2}\n";
            }
            csvContent += "\n";

            // Revenue by Category
            csvContent += "REVENUE BY CATEGORY\n";
            csvContent += "Category,Revenue,Percentage\n";
            foreach (var category in categoryRevenue)
            {
                csvContent += $"{category.Category},${category.Revenue:F2},{category.Percentage:F1}%\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"dashboard-summary-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }

        // Helper methods for data retrieval
        private async Task<object> GetSalesReportData(DateTime startDate, DateTime endDate)
        {
            var payments = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .ThenInclude(e => e.Organizer)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .Take(1000) // Limit for export
                .Select(p => new
                {
                    Date = p.PaymentDate,
                    EventTitle = p.Ticket.Event.Title,
                    Category = p.Ticket.Event.Category,
                    Organizer = p.Ticket.Event.Organizer != null ? p.Ticket.Event.Organizer.FullName : "Unknown",
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount,
                    TransactionId = p.TransactionId ?? ""
                })
                .ToListAsync();

            var totalAmount = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            return new
            {
                TotalAmount = totalAmount,
                TotalTransactions = payments.Count,
                Transactions = payments
            };
        }

        private async Task<object> GetUserReportData()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.CreatedAt,
                    u.IsActive,
                    u.LastLogin
                })
                .ToListAsync();

            var events = await _context.Events.ToListAsync();
            var tickets = await _context.Tickets.Where(t => t.IsPaid).ToListAsync();

            var userDetails = new List<object>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(new ApplicationUser { Id = user.Id });
                var eventsOrganized = events.Count(e => e.OrganizerId == user.Id);
                var ticketsPurchased = tickets.Count(t => t.CustomerId == user.Id);
                var totalSpent = tickets.Where(t => t.CustomerId == user.Id).Sum(t => t.TotalAmount);

                userDetails.Add(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    Role = userRoles.FirstOrDefault() ?? "Customer",
                    user.CreatedAt,
                    Status = user.IsActive ? "Active" : "Inactive",
                    EventsOrganized = eventsOrganized,
                    TicketsPurchased = ticketsPurchased,
                    TotalSpent = totalSpent,
                    user.LastLogin
                });
            }

            return new
            {
                TotalUsers = users.Count,
                ActiveUsers = users.Count(u => u.IsActive),
                Users = userDetails
            };
        }

        private async Task<object> GetEventReportData(DateTime startDate, DateTime endDate)
        {
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .Include(e => e.Tickets)
                .Include(e => e.Prices)
                .Where(e => e.EventDate >= startDate && e.EventDate <= endDate)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Category,
                    Organizer = e.Organizer != null ? e.Organizer.FullName : "Unknown",
                    Venue = e.Venue != null ? e.Venue.Name : "Unknown",
                    e.EventDate,
                    Status = e.IsPublished ? "Published" : "Draft",
                    TicketsSold = e.Tickets != null ? e.Tickets.Count(t => t.IsPaid) : 0,
                    TotalCapacity = e.Prices != null ? e.Prices.Sum(p => p.Stock) : 0,
                    Revenue = e.Tickets != null ? e.Tickets.Where(t => t.IsPaid).Sum(t => t.TotalAmount) : 0,
                    e.CreatedAt
                })
                .ToListAsync();

            return new
            {
                TotalEvents = events.Count,
                PublishedEvents = events.Count(e => e.Status == "Published"),
                TotalRevenue = events.Sum(e => e.Revenue),
                Events = events
            };
        }

        private async Task<object> GetRevenueReportData(DateTime startDate, DateTime endDate)
        {
            var monthlyRevenue = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed")
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.Amount),
                    TransactionCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var categoryRevenue = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed")
                .GroupBy(p => p.Ticket.Event.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return new
            {
                TotalRevenue = monthlyRevenue.Sum(m => m.Revenue),
                MonthlyBreakdown = monthlyRevenue,
                CategoryBreakdown = categoryRevenue
            };
        }

        private async Task<object> GetDashboardSummaryData()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var totalTicketsSold = await _context.Tickets.CountAsync(t => t.IsPaid);
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentActivity = new
            {
                NewUsers = await _context.Users.CountAsync(u => u.CreatedAt >= thirtyDaysAgo),
                NewEvents = await _context.Events.CountAsync(e => e.CreatedAt >= thirtyDaysAgo),
                RecentSales = await _context.Tickets.CountAsync(t => t.IsPaid && t.PurchaseDate >= thirtyDaysAgo),
                RecentRevenue = await _context.Payments
                    .Where(p => p.Status == "Completed" && p.PaymentDate >= thirtyDaysAgo)
                    .SumAsync(p => p.Amount)
            };

            var topEvents = await _context.Events
                .Include(e => e.Tickets)
                .Where(e => e.Tickets.Any(t => t.IsPaid))
                .Select(e => new
                {
                    e.Title,
                    TicketsSold = e.Tickets.Count(t => t.IsPaid),
                    Revenue = e.Tickets.Where(t => t.IsPaid).Sum(t => t.TotalAmount)
                })
                .OrderByDescending(e => e.Revenue)
                .Take(5)
                .ToListAsync();

            var categoryRevenue = await _context.Payments
                .Include(p => p.Ticket)
                .ThenInclude(t => t.Event)
                .Where(p => p.Status == "Completed")
                .GroupBy(p => p.Ticket.Event.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var revenueByCategory = categoryRevenue.Select(c => new
            {
                c.Category,
                c.Revenue,
                Percentage = totalRevenue > 0 ? (double)(c.Revenue / totalRevenue) * 100 : 0
            }).ToList();

            return new
            {
                TotalUsers = totalUsers,
                TotalEvents = totalEvents,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                RecentActivity = recentActivity,
                TopEvents = topEvents,
                RevenueByCategory = revenueByCategory
            };
        }

        #region Private Helper Methods

        private List<SalesByPeriod> GetSalesByPeriod(List<Payment> payments, string groupBy)
        {
            return groupBy.ToLower() switch
            {
                "daily" => payments
                    .GroupBy(p => p.PaymentDate.Date)
                    .Select(g => new SalesByPeriod
                    {
                        Period = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(p => p.Amount),
                        TicketsSold = g.Count(),
                        Transactions = g.Count()
                    })
                    .OrderBy(x => x.Period)
                    .ToList(),
                "weekly" => payments
                    .GroupBy(p => GetWeekStart(p.PaymentDate))
                    .Select(g => new SalesByPeriod
                    {
                        Period = $"Week of {g.Key:yyyy-MM-dd}",
                        Revenue = g.Sum(p => p.Amount),
                        TicketsSold = g.Count(),
                        Transactions = g.Count()
                    })
                    .OrderBy(x => x.Period)
                    .ToList(),
                "yearly" => payments
                    .GroupBy(p => p.PaymentDate.Year)
                    .Select(g => new SalesByPeriod
                    {
                        Period = g.Key.ToString(),
                        Revenue = g.Sum(p => p.Amount),
                        TicketsSold = g.Count(),
                        Transactions = g.Count()
                    })
                    .OrderBy(x => x.Period)
                    .ToList(),
                _ => payments // monthly (default)
                    .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                    .Select(g => new SalesByPeriod
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:00}",
                        Revenue = g.Sum(p => p.Amount),
                        TicketsSold = g.Count(),
                        Transactions = g.Count()
                    })
                    .OrderBy(x => x.Period)
                    .ToList()
            };
        }

        private List<UserRegistrationTrend> GetUserRegistrationTrend(
            List<ApplicationUser> users,
            List<IdentityUserRole<string>> userRoles,
            string? organizerRoleId,
            string? customerRoleId,
            DateTime startDate,
            DateTime endDate)
        {
            return users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new UserRegistrationTrend
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:00}",
                    NewRegistrations = g.Count(),
                    NewOrganizers = organizerRoleId != null ? g.Count(u => userRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == organizerRoleId)) : 0,
                    NewCustomers = customerRoleId != null ? g.Count(u => userRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId)) : 0
                })
                .OrderBy(x => x.Period)
                .ToList();
        }

        private List<EventsByPeriod> GetEventsByPeriod(List<Event> events, string groupBy)
        {
            return events
                .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
                .Select(g => new EventsByPeriod
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:00}",
                    EventsCreated = g.Count(),
                    EventsPublished = g.Count(e => e.IsPublished),
                    EventsHeld = g.Count(e => e.EventDate < DateTime.UtcNow)
                })
                .OrderBy(x => x.Period)
                .ToList();
        }

        private List<RevenueByPeriod> GetRevenueByPeriod(List<Payment> payments, string groupBy)
        {
            var groupedData = payments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new RevenueByPeriod
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:00}",
                    Revenue = g.Sum(p => p.Amount),
                    PendingRevenue = g.Where(p => p.Status == "Pending").Sum(p => p.Amount),
                    CompletedRevenue = g.Where(p => p.Status == "Completed").Sum(p => p.Amount),
                    TransactionCount = g.Count(),
                    GrowthRate = 0 // Will be calculated below
                })
                .OrderBy(x => x.Period)
                .ToList();

            // Calculate growth rate
            for (int i = 1; i < groupedData.Count; i++)
            {
                var current = groupedData[i];
                var previous = groupedData[i - 1];
                if (previous.CompletedRevenue > 0)
                {
                    current.GrowthRate = ((current.CompletedRevenue - previous.CompletedRevenue) / previous.CompletedRevenue) * 100;
                }
            }

            return groupedData;
        }

        private string GetUserRole(string userId, List<IdentityUserRole<string>> userRoles, List<IdentityRole> roles)
        {
            var roleId = userRoles.FirstOrDefault(ur => ur.UserId == userId)?.RoleId;
            return roleId != null ? roles.FirstOrDefault(r => r.Id == roleId)?.Name ?? "Unknown" : "Unknown";
        }

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private decimal CalculateGrowthRate(List<RevenueByPeriod> revenueData)
        {
            if (revenueData.Count < 2) return 0;

            var recent = revenueData.TakeLast(3).ToList();
            if (recent.Count < 2) return 0;

            var firstPeriod = recent.First().CompletedRevenue;
            var lastPeriod = recent.Last().CompletedRevenue;

            if (firstPeriod == 0) return 0;

            return ((lastPeriod - firstPeriod) / firstPeriod) * 100;
        }

        #endregion
    }
}