using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Repositories;

namespace StartEvent_API.Business
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IDiscountRepository _discountRepository;
        private readonly ILoyaltyPointRepository _loyaltyPointRepository;
        private readonly ApplicationDbContext _context;
        private readonly IQrService _qrService; // Add this line

        public TicketService(
            ITicketRepository ticketRepository,
            IPaymentRepository paymentRepository,
            IDiscountRepository discountRepository,
            ILoyaltyPointRepository loyaltyPointRepository,
            ApplicationDbContext context,
            IQrService qrService)
        {
            _ticketRepository = ticketRepository;
            _paymentRepository = paymentRepository;
            _discountRepository = discountRepository;
            _loyaltyPointRepository = loyaltyPointRepository;
            _context = context;
            _qrService = qrService; // Assign qrService
        }

        public async Task<Ticket?> GetTicketByIdAsync(Guid id)
        {
            return await _ticketRepository.GetByIdAsync(id);
        }

        public async Task<Ticket?> GetTicketByNumberAsync(string ticketNumber)
        {
            return await _ticketRepository.GetByTicketNumberAsync(ticketNumber);
        }

        public async Task<IEnumerable<Ticket>> GetCustomerTicketsAsync(string customerId, int page = 1, int pageSize = 10)
        {
            return await _ticketRepository.GetByCustomerIdAsync(customerId, page, pageSize);
        }

        public async Task<Ticket> BookTicketAsync(string customerId, Guid eventId, Guid eventPriceId, int quantity, string? discountCode = null, bool useLoyaltyPoints = false)
        {
            // Get event price details
            var eventPrice = await _context.EventPrices
                .Include(ep => ep.Event)
                .FirstOrDefaultAsync(ep => ep.Id == eventPriceId && ep.EventId == eventId);

            if (eventPrice == null)
                throw new ArgumentException("Event price not found");

            if (eventPrice.Stock < quantity)
                throw new InvalidOperationException("Insufficient ticket stock");

            // Calculate base amount
            var baseAmount = eventPrice.Price * quantity;
            var discountAmount = 0m;

            // Apply discount if provided
            if (!string.IsNullOrEmpty(discountCode))
            {
                discountAmount = await _discountRepository.CalculateDiscountAsync(discountCode, baseAmount, eventId);
            }

            // Calculate loyalty points discount
            var loyaltyDiscount = 0m;
            if (useLoyaltyPoints)
            {
                var totalPoints = await _loyaltyPointRepository.GetTotalPointsByCustomerIdAsync(customerId);
                var pointsToUse = Math.Min(totalPoints, (int)(baseAmount * 0.1m)); // Max 10% discount
                loyaltyDiscount = pointsToUse;
            }

            var totalAmount = baseAmount - discountAmount - loyaltyDiscount;

            // Create ticket
            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                EventId = eventId,
                EventPriceId = eventPriceId,
                TicketNumber = await _ticketRepository.GenerateTicketNumberAsync(),
                TicketCode = await _ticketRepository.GenerateTicketCodeAsync(),
                Quantity = quantity,
                TotalAmount = totalAmount,
                PurchaseDate = DateTime.UtcNow,
                IsPaid = false,
                QrCodePath = string.Empty
            };

            // Update stock
            eventPrice.Stock -= quantity;

            // Save ticket
            await _ticketRepository.CreateAsync(ticket);
            await _context.SaveChangesAsync();

            // Generate QR code
            ticket.QrCodePath = await GenerateQRCodeAsync(ticket.Id);
            await _ticketRepository.UpdateAsync(ticket);

            return ticket;
        }

        public async Task<bool> ApplyPromotionAsync(Guid ticketId, string discountCode)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null) return false;

            var discountAmount = await _discountRepository.CalculateDiscountAsync(discountCode, ticket.TotalAmount, ticket.EventId);
            if (discountAmount <= 0) return false;

            ticket.TotalAmount -= discountAmount;
            await _ticketRepository.UpdateAsync(ticket);

            return true;
        }

        public async Task<bool> UseLoyaltyPointsAsync(Guid ticketId, int points)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null) return false;

            var totalPoints = await _loyaltyPointRepository.GetTotalPointsByCustomerIdAsync(ticket.CustomerId);
            if (totalPoints < points) return false;

            var discountAmount = Math.Min(points, ticket.TotalAmount);
            ticket.TotalAmount -= discountAmount;

            await _ticketRepository.UpdateAsync(ticket);
            await _loyaltyPointRepository.RedeemPointsAsync(ticket.CustomerId, points);

            return true;
        }

        public async Task<string> GenerateQRCodeAsync(Guid ticketId)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null) return string.Empty;

            // Use the QrService to generate the QR code
            var result = await _qrService.GenerateQrCodeAsync(ticket.Id, ticket.CustomerId);

            if (result.Success && !string.IsNullOrEmpty(result.QrCodePath))
            {
                return result.QrCodePath;
            }
            
            return string.Empty; // Return empty string if QR code generation failed
        }

        public async Task<bool> ValidateTicketAsync(string ticketCode)
        {
            var ticket = await _ticketRepository.GetByTicketNumberAsync(ticketCode);
            return ticket != null && ticket.IsPaid;
        }
    }
}
