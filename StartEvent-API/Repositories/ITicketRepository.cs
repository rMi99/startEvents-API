using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public interface ITicketRepository
    {
        Task<Ticket?> GetByIdAsync(Guid id);
        Task<Ticket?> GetByTicketNumberAsync(string ticketNumber);
        Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId, int page = 1, int pageSize = 10);
        Task<IEnumerable<Ticket>> GetByEventIdAsync(Guid eventId);
        Task<Ticket> CreateAsync(Ticket ticket);
        Task<Ticket> UpdateAsync(Ticket ticket);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<int> GetTotalTicketsForEventAsync(Guid eventId);
        Task<decimal> GetTotalRevenueForEventAsync(Guid eventId);
        Task<IEnumerable<Ticket>> GetTicketsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<string> GenerateTicketNumberAsync();
        Task<string> GenerateTicketCodeAsync();
    }
}
