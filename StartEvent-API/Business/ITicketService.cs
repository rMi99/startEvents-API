using StartEvent_API.Data.Entities;

namespace StartEvent_API.Business
{
    public interface ITicketService
    {
        Task<Ticket?> GetTicketByIdAsync(Guid id);
        Task<Ticket?> GetTicketByNumberAsync(string ticketNumber);
        Task<IEnumerable<Ticket>> GetCustomerTicketsAsync(string customerId, int page = 1, int pageSize = 10);
        Task<Ticket> BookTicketAsync(string customerId, Guid eventId, Guid eventPriceId, int quantity, string? discountCode = null, bool useLoyaltyPoints = false);
        Task<bool> ApplyPromotionAsync(Guid ticketId, string discountCode);
        Task<bool> UseLoyaltyPointsAsync(Guid ticketId, int points);
        Task<string> GenerateQRCodeAsync(Guid ticketId);
        Task<bool> ValidateTicketAsync(string ticketCode);
    }
}
