using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Ticket?> GetByIdAsync(Guid id)
        {
            return await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Ticket?> GetByTicketNumberAsync(string ticketNumber)
        {
            return await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);
        }

        public async Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId, int page = 1, int pageSize = 10)
        {
            return await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.PurchaseDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetByEventIdAsync(Guid eventId)
        {
            return await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .Where(t => t.EventId == eventId)
                .ToListAsync();
        }

        public async Task<Ticket> CreateAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<Ticket> UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return false;

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Tickets.AnyAsync(t => t.Id == id);
        }

        public async Task<int> GetTotalTicketsForEventAsync(Guid eventId)
        {
            return await _context.Tickets
                .Where(t => t.EventId == eventId && t.IsPaid)
                .SumAsync(t => t.Quantity);
        }

        public async Task<decimal> GetTotalRevenueForEventAsync(Guid eventId)
        {
            return await _context.Tickets
                .Where(t => t.EventId == eventId && t.IsPaid)
                .SumAsync(t => t.TotalAmount);
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .Where(t => t.PurchaseDate >= startDate && t.PurchaseDate <= endDate)
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();
        }

        public async Task<string> GenerateTicketNumberAsync()
        {
            var prefix = "TKT";
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return await Task.FromResult($"{prefix}{timestamp}{random}");
        }

        public async Task<string> GenerateTicketCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return await Task.FromResult(new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray()));
        }
    }
}
