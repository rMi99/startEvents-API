using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public class QrRepository : IQrRepository
    {
        private readonly ApplicationDbContext _context;

        public QrRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId)
        {
            return await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
        }

        public async Task<Ticket?> GetTicketByCodeAsync(string ticketCode)
        {
            return await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Event)
                .Include(t => t.EventPrice)
                .FirstOrDefaultAsync(t => t.TicketCode == ticketCode);
        }

        public async Task<Ticket> UpdateTicketAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<bool> TicketExistsAsync(Guid ticketId)
        {
            return await _context.Tickets.AnyAsync(t => t.Id == ticketId);
        }

        public async Task<bool> IsTicketValidAsync(Guid ticketId)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            return ticket != null && ticket.IsPaid;
        }

        public async Task<string> GenerateUniqueTicketCodeAsync()
        {
            string ticketCode;
            bool isUnique = false;
            
            do
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
                var random = new Random().Next(1000, 9999);
                var prefix = "TKT";
                ticketCode = $"{prefix}-{timestamp}-{random}";
                
                isUnique = !await _context.Tickets.AnyAsync(t => t.TicketCode == ticketCode);
            } while (!isUnique);
            
            return ticketCode;
        }
    }
}
