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
        private readonly ILoyaltyPointReservationRepository _loyaltyPointReservationRepository;
        private readonly ApplicationDbContext _context;
        private readonly IQrService _qrService; // Add this line

        public TicketService(
            ITicketRepository ticketRepository,
            IPaymentRepository paymentRepository,
            IDiscountRepository discountRepository,
            ILoyaltyPointRepository loyaltyPointRepository,
            ILoyaltyPointReservationRepository loyaltyPointReservationRepository,
            ApplicationDbContext context,
            IQrService qrService)
        {
            _ticketRepository = ticketRepository;
            _paymentRepository = paymentRepository;
            _discountRepository = discountRepository;
            _loyaltyPointRepository = loyaltyPointRepository;
            _loyaltyPointReservationRepository = loyaltyPointReservationRepository;
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

        public async Task<Ticket> BookTicketAsync(string customerId, Guid eventId, Guid eventPriceId, int quantity, string? discountCode = null, bool useLoyaltyPoints = false, int pointsToRedeem = 0)
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

            // Calculate loyalty points discount (but don't redeem yet - just calculate)
            var loyaltyDiscount = 0m;
            var pointsToReserve = 0;
            if (useLoyaltyPoints && pointsToRedeem > 0)
            {
                var availablePoints = await _loyaltyPointRepository.GetAvailablePointsByCustomerIdAsync(customerId);
                
                // Use the specific points requested, but validate against available points and ticket amount
                pointsToReserve = Math.Min(Math.Min(pointsToRedeem, availablePoints), (int)baseAmount);
                loyaltyDiscount = pointsToReserve; // 1 point = 1 LKR discount
            }

            var totalAmount = Math.Max(0, baseAmount - discountAmount - loyaltyDiscount);

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
                QrCodePath = string.Empty,
                PointsRedeemed = 0, // Will be set after payment confirmation
                PointsEarned = 0 // Will be set after successful payment
            };

            // Update stock
            eventPrice.Stock -= quantity;

            // Save ticket
            await _ticketRepository.CreateAsync(ticket);
            
            // Reserve loyalty points if requested (don't redeem yet)
            if (pointsToReserve > 0)
            {
                var reservation = new LoyaltyPointReservation
                {
                    CustomerId = customerId,
                    TicketId = ticket.Id,
                    ReservedPoints = pointsToReserve
                };
                await _loyaltyPointReservationRepository.CreateAsync(reservation);
            }
            
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

        public async Task<bool> AwardLoyaltyPointsAsync(Guid ticketId)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null || !ticket.IsPaid || ticket.PointsEarned > 0)
                return false;

            // Calculate points earned: 1 point per 10 LKR
            var pointsEarned = (int)(ticket.TotalAmount / 10);
            
            if (pointsEarned > 0)
            {
                // Update ticket with points earned
                ticket.PointsEarned = pointsEarned;
                await _ticketRepository.UpdateAsync(ticket);

                // Add points to customer's loyalty account
                await _loyaltyPointRepository.AddPointsAsync(
                    ticket.CustomerId, 
                    pointsEarned, 
                    $"Earned from ticket #{ticket.TicketNumber}");

                return true;
            }

            return false;
        }

        public async Task<bool> ReserveLoyaltyPointsAsync(Guid ticketId, int pointsToReserve)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null) return false;

            var availablePoints = await _loyaltyPointRepository.GetAvailablePointsByCustomerIdAsync(ticket.CustomerId);
            if (availablePoints < pointsToReserve) return false;

            // Check if there's already a reservation for this ticket
            var existingReservation = await _loyaltyPointReservationRepository.GetByTicketIdAsync(ticketId);
            if (existingReservation != null)
            {
                // Update existing reservation
                existingReservation.ReservedPoints = pointsToReserve;
                existingReservation.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
                await _loyaltyPointReservationRepository.UpdateAsync(existingReservation);
            }
            else
            {
                // Create new reservation
                var reservation = new LoyaltyPointReservation
                {
                    CustomerId = ticket.CustomerId,
                    TicketId = ticketId,
                    ReservedPoints = pointsToReserve
                };
                await _loyaltyPointReservationRepository.CreateAsync(reservation);
            }

            return true;
        }

        public async Task<bool> ConfirmLoyaltyPointsRedemptionAsync(Guid ticketId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketId);
                if (ticket == null) return false;

                // Calculate points earned BEFORE any redemption (based on original total amount)
                var pointsEarned = (int)(ticket.TotalAmount / 10); // 1 point per 10 LKR spent

                // Handle loyalty points reservation if exists
                var reservation = await _loyaltyPointReservationRepository.GetByTicketIdAsync(ticketId);
                if (reservation != null && !reservation.IsExpired)
                {
                    // Redeem the reserved points
                    await _loyaltyPointRepository.RedeemPointsAsync(ticket.CustomerId, reservation.ReservedPoints);
                    
                    // Update ticket with redeemed points
                    ticket.PointsRedeemed = reservation.ReservedPoints;
                    
                    // Mark reservation as confirmed
                    reservation.IsConfirmed = true;
                    await _loyaltyPointReservationRepository.UpdateAsync(reservation);
                }

                // Award points for this purchase (always award based on total spent)
                if (pointsEarned > 0)
                {
                    ticket.PointsEarned = pointsEarned;
                    await _loyaltyPointRepository.AddPointsAsync(
                        ticket.CustomerId,
                        pointsEarned,
                        $"Earned from ticket #{ticket.TicketNumber} - {pointsEarned} points");
                }

                // Mark ticket as paid
                ticket.IsPaid = true;
                await _ticketRepository.UpdateAsync(ticket);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> RollbackLoyaltyPointsAsync(Guid ticketId)
        {
            var reservation = await _loyaltyPointReservationRepository.GetByTicketIdAsync(ticketId);
            if (reservation == null) return false;

            // Simply delete the reservation to free up the points
            await _loyaltyPointReservationRepository.DeleteAsync(reservation.Id);
            return true;
        }
    }
}
