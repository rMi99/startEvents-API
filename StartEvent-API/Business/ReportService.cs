using StartEvent_API.Data.Entities;
using StartEvent_API.Repositories;
using StartEvent_API.Models.Reports;

namespace StartEvent_API.Business
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<IEnumerable<Ticket>> GetOrganizerTicketSalesAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _reportRepository.GetOrganizerTicketSalesAsync(organizerId, startDate, endDate);
        }

        public async Task<decimal> GetOrganizerRevenueAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _reportRepository.GetOrganizerRevenueAsync(organizerId, startDate, endDate);
        }

        public async Task<IEnumerable<Event>> GetOrganizerEventsAsync(string organizerId)
        {
            return await _reportRepository.GetOrganizerEventsAsync(organizerId);
        }

        public async Task<IEnumerable<Ticket>> GetSystemWideTicketSalesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _reportRepository.GetSystemWideTicketSalesAsync(startDate, endDate);
        }

        public async Task<decimal> GetSystemWideRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _reportRepository.GetSystemWideRevenueAsync(startDate, endDate);
        }

        public async Task<IEnumerable<ApplicationUser>> GetSystemUsersAsync()
        {
            return await _reportRepository.GetSystemUsersAsync();
        }

        public async Task<IEnumerable<Event>> GetAllEventsWithMetricsAsync()
        {
            return await _reportRepository.GetAllEventsWithMetricsAsync();
        }

        public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
        {
            return await _reportRepository.GetUserStatisticsAsync();
        }

        public async Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year)
        {
            return await _reportRepository.GetRevenueByMonthAsync(year);
        }

        public async Task<Dictionary<string, int>> GetTicketSalesByCategoryAsync()
        {
            return await _reportRepository.GetTicketSalesByCategoryAsync();
        }

        public async Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count = 10)
        {
            return await _reportRepository.GetRecentPaymentsAsync(count);
        }

        public async Task<object> GetSystemMonitoringDataAsync()
        {
            var totalUsers = await _reportRepository.GetTotalActiveUsersAsync();
            var totalEvents = await _reportRepository.GetTotalEventsAsync();
            var totalTicketsSold = await _reportRepository.GetTotalTicketsSoldAsync();
            var totalRevenue = await _reportRepository.GetSystemWideRevenueAsync();
            var recentPayments = await _reportRepository.GetRecentPaymentsAsync(5);
            var revenueByMonth = await _reportRepository.GetRevenueByMonthAsync(DateTime.UtcNow.Year);
            var ticketSalesByCategory = await _reportRepository.GetTicketSalesByCategoryAsync();

            return new
            {
                SystemOverview = new
                {
                    TotalUsers = totalUsers,
                    TotalEvents = totalEvents,
                    TotalTicketsSold = totalTicketsSold,
                    TotalRevenue = totalRevenue,
                    Timestamp = DateTime.UtcNow
                },
                RecentActivity = new
                {
                    RecentPayments = recentPayments.Select(p => new
                    {
                        p.Id,
                        p.Amount,
                        p.PaymentDate,
                        p.Status,
                        CustomerEmail = p.Customer.Email,
                        TicketNumber = p.Ticket.TicketNumber
                    })
                },
                Analytics = new
                {
                    RevenueByMonth = revenueByMonth,
                    TicketSalesByCategory = ticketSalesByCategory
                }
            };
        }

        // Enhanced Organizer Report Methods

        public async Task<SalesReportDto> GetOrganizerSalesReportAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _reportRepository.GetOrganizerSalesReportAsync(organizerId, startDate, endDate);
        }

        public async Task<Dictionary<string, decimal>> GetOrganizerRevenueByMonthAsync(string organizerId, int year)
        {
            return await _reportRepository.GetOrganizerRevenueByMonthAsync(organizerId, year);
        }

        public async Task<List<SalesByEvent>> GetOrganizerEventPerformanceAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _reportRepository.GetOrganizerEventPerformanceAsync(organizerId, startDate, endDate);
        }

        public async Task<List<SalesByPeriod>> GetOrganizerSalesByPeriodAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null, string periodType = "monthly")
        {
            return await _reportRepository.GetOrganizerSalesByPeriodAsync(organizerId, startDate, endDate, periodType);
        }

        public async Task<PaymentMethodBreakdown> GetOrganizerPaymentMethodsAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _reportRepository.GetOrganizerPaymentMethodsAsync(organizerId, startDate, endDate);
        }

        public async Task<List<EventPerformance>> GetOrganizerEventSummaryAsync(string organizerId)
        {
            return await _reportRepository.GetOrganizerEventSummaryAsync(organizerId);
        }
    }
}
