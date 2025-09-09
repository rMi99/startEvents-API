using StartEvent_API.Data.Entities;

namespace StartEvent_API.Business
{
    public interface IReportService
    {
        // Organizer Reports
        Task<IEnumerable<Ticket>> GetOrganizerTicketSalesAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetOrganizerRevenueAsync(string organizerId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<Event>> GetOrganizerEventsAsync(string organizerId);
        
        // Admin Reports
        Task<IEnumerable<Ticket>> GetSystemWideTicketSalesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetSystemWideRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<ApplicationUser>> GetSystemUsersAsync();
        Task<IEnumerable<Event>> GetAllEventsWithMetricsAsync();
        Task<Dictionary<string, int>> GetUserStatisticsAsync();
        Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year);
        Task<Dictionary<string, int>> GetTicketSalesByCategoryAsync();
        Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count = 10);
        Task<object> GetSystemMonitoringDataAsync();
    }
}
