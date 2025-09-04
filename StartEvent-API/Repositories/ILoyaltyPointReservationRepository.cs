using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public interface ILoyaltyPointReservationRepository
    {
        Task<LoyaltyPointReservation?> GetByTicketIdAsync(Guid ticketId);
        Task<LoyaltyPointReservation> CreateAsync(LoyaltyPointReservation reservation);
        Task<LoyaltyPointReservation> UpdateAsync(LoyaltyPointReservation reservation);
        Task<bool> DeleteAsync(Guid id);
        Task<int> GetTotalReservedPointsByCustomerIdAsync(string customerId);
        Task<bool> CleanupExpiredReservationsAsync();
    }
}
