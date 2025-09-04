using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public class LoyaltyPointReservationRepository : ILoyaltyPointReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public LoyaltyPointReservationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LoyaltyPointReservation?> GetByTicketIdAsync(Guid ticketId)
        {
            return await _context.LoyaltyPointReservations
                .Include(r => r.Customer)
                .Include(r => r.Ticket)
                .FirstOrDefaultAsync(r => r.TicketId == ticketId && !r.IsConfirmed);
        }

        public async Task<LoyaltyPointReservation> CreateAsync(LoyaltyPointReservation reservation)
        {
            _context.LoyaltyPointReservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<LoyaltyPointReservation> UpdateAsync(LoyaltyPointReservation reservation)
        {
            _context.LoyaltyPointReservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var reservation = await _context.LoyaltyPointReservations.FindAsync(id);
            if (reservation == null) return false;

            _context.LoyaltyPointReservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalReservedPointsByCustomerIdAsync(string customerId)
        {
            return await _context.LoyaltyPointReservations
                .Where(r => r.CustomerId == customerId && !r.IsConfirmed && !r.IsExpired)
                .SumAsync(r => r.ReservedPoints);
        }

        public async Task<bool> CleanupExpiredReservationsAsync()
        {
            var expiredReservations = await _context.LoyaltyPointReservations
                .Where(r => r.ExpiresAt < DateTime.UtcNow && !r.IsConfirmed)
                .ToListAsync();

            if (expiredReservations.Any())
            {
                _context.LoyaltyPointReservations.RemoveRange(expiredReservations);
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
