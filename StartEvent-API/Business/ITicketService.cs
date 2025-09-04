using StartEvent_API.Data.Entities;

namespace StartEvent_API.Business
{
    public interface ITicketService
    {
        Task<Ticket?> GetTicketByIdAsync(Guid id);
        Task<Ticket?> GetTicketByNumberAsync(string ticketNumber);
        Task<IEnumerable<Ticket>> GetCustomerTicketsAsync(string customerId, int page = 1, int pageSize = 10);
        Task<Ticket> BookTicketAsync(string customerId, Guid eventId, Guid eventPriceId, int quantity, string? discountCode = null, bool useLoyaltyPoints = false, int pointsToRedeem = 0);
        Task<bool> ApplyPromotionAsync(Guid ticketId, string discountCode);
        Task<bool> UseLoyaltyPointsAsync(Guid ticketId, int points);
        Task<string> GenerateQRCodeAsync(Guid ticketId);
        Task<bool> ValidateTicketAsync(string ticketCode);
        Task<bool> AwardLoyaltyPointsAsync(Guid ticketId);
        Task<bool> ReserveLoyaltyPointsAsync(Guid ticketId, int pointsToReserve);
        Task<bool> ConfirmLoyaltyPointsRedemptionAsync(Guid ticketId);
        Task<bool> RollbackLoyaltyPointsAsync(Guid ticketId);
    }
}
