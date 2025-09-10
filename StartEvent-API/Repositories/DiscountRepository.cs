using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly ApplicationDbContext _context;

        public DiscountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Discount?> GetByIdAsync(Guid id)
        {
            return await _context.Discounts
                .Include(d => d.Event)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Discount?> GetByCodeAsync(string code)
        {
            return await _context.Discounts
                .Include(d => d.Event)
                .FirstOrDefaultAsync(d => d.Code == code);
        }

        public async Task<IEnumerable<Discount>> GetByEventIdAsync(Guid eventId)
        {
            return await _context.Discounts
                .Where(d => d.EventId == eventId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Discount>> GetActiveDiscountsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Discounts
                .Where(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now)
                .ToListAsync();
        }

        public async Task<Discount> CreateAsync(Discount discount)
        {
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
            return discount;
        }

        public async Task<Discount> UpdateAsync(Discount discount)
        {
            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();
            return discount;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null) return false;

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Discounts.AnyAsync(d => d.Id == id);
        }

        public async Task<bool> IsCodeValidAsync(string code, Guid? eventId = null)
        {
            var now = DateTime.UtcNow;
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Code == code && d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);

            if (discount == null) return false;

            // If eventId is provided, check if discount is for that specific event or global
            if (eventId.HasValue)
            {
                return discount.EventId == null || discount.EventId == eventId.Value;
            }

            return true;
        }

        public async Task<decimal> CalculateDiscountAsync(string code, decimal amount, Guid? eventId = null)
        {
            var discount = await GetByCodeAsync(code);
            if (discount == null || !await IsCodeValidAsync(code, eventId))
                return 0;

            if (discount.Type == "Percent")
            {
                return amount * (discount.Value / 100);
            }
            else // Amount
            {
                return Math.Min(discount.Value, amount);
            }
        }
    }
}
