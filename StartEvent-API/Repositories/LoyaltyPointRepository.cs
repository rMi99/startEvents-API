using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public class LoyaltyPointRepository : ILoyaltyPointRepository
    {
        private readonly ApplicationDbContext _context;

        public LoyaltyPointRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LoyaltyPoint?> GetByIdAsync(Guid id)
        {
            return await _context.LoyaltyPoints
                .Include(lp => lp.Customer)
                .FirstOrDefaultAsync(lp => lp.Id == id);
        }

        public async Task<IEnumerable<LoyaltyPoint>> GetByCustomerIdAsync(string customerId)
        {
            return await _context.LoyaltyPoints
                .Include(lp => lp.Customer)
                .Where(lp => lp.CustomerId == customerId)
                .OrderByDescending(lp => lp.EarnedDate)
                .ToListAsync();
        }

        public async Task<int> GetTotalPointsByCustomerIdAsync(string customerId)
        {
            return await _context.LoyaltyPoints
                .Where(lp => lp.CustomerId == customerId)
                .SumAsync(lp => lp.Points);
        }

        public async Task<LoyaltyPoint> CreateAsync(LoyaltyPoint loyaltyPoint)
        {
            _context.LoyaltyPoints.Add(loyaltyPoint);
            await _context.SaveChangesAsync();
            return loyaltyPoint;
        }

        public async Task<LoyaltyPoint> UpdateAsync(LoyaltyPoint loyaltyPoint)
        {
            _context.LoyaltyPoints.Update(loyaltyPoint);
            await _context.SaveChangesAsync();
            return loyaltyPoint;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var loyaltyPoint = await _context.LoyaltyPoints.FindAsync(id);
            if (loyaltyPoint == null) return false;

            _context.LoyaltyPoints.Remove(loyaltyPoint);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.LoyaltyPoints.AnyAsync(lp => lp.Id == id);
        }

        public async Task<bool> RedeemPointsAsync(string customerId, int points)
        {
            var totalPoints = await GetTotalPointsByCustomerIdAsync(customerId);
            if (totalPoints < points) return false;

            var loyaltyPoint = new LoyaltyPoint
            {
                CustomerId = customerId,
                Points = -points, // Negative points for redemption
                Description = $"Redeemed {points} points"
            };

            await CreateAsync(loyaltyPoint);
            return true;
        }

        public async Task<bool> AddPointsAsync(string customerId, int points, string description)
        {
            var loyaltyPoint = new LoyaltyPoint
            {
                CustomerId = customerId,
                Points = points,
                Description = description
            };

            await CreateAsync(loyaltyPoint);
            return true;
        }

        public async Task<int> GetAvailablePointsByCustomerIdAsync(string customerId)
        {
            var totalPoints = await GetTotalPointsByCustomerIdAsync(customerId);
            
            try 
            {
                // Clean up expired reservations first
                var expiredReservations = await _context.LoyaltyPointReservations
                    .Where(r => r.CustomerId == customerId && !r.IsConfirmed && r.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredReservations.Any())
                {
                    _context.LoyaltyPointReservations.RemoveRange(expiredReservations);
                    await _context.SaveChangesAsync();
                }

                // Subtract only active (non-expired) reserved points
                var activeReservedPoints = await _context.LoyaltyPointReservations
                    .Where(r => r.CustomerId == customerId && !r.IsConfirmed && r.ExpiresAt > DateTime.UtcNow)
                    .SumAsync(r => r.ReservedPoints);

                var availablePoints = Math.Max(0, totalPoints - activeReservedPoints);
                
                // Debug logging
                Console.WriteLine($"Customer {customerId}: Total={totalPoints}, Reserved={activeReservedPoints}, Available={availablePoints}");
                
                return availablePoints;
            }
            catch (Exception ex)
            {
                // If there's an issue with reservations table, return total points as fallback
                Console.WriteLine($"Error in GetAvailablePointsByCustomerIdAsync: {ex.Message}");
                return totalPoints;
            }
        }
    }
}
