using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Ticket)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Ticket)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<IEnumerable<Payment>> GetByCustomerIdAsync(string customerId)
        {
            return await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Ticket)
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByTicketIdAsync(Guid ticketId)
        {
            return await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Ticket)
                .Where(p => p.TicketId == ticketId)
                .ToListAsync();
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return false;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Payments.AnyAsync(p => p.Id == id);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);
        }

        public async Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Where(p => p.Status == "Completed" && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.Amount);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status)
        {
            return await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Ticket)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }
    }
}
