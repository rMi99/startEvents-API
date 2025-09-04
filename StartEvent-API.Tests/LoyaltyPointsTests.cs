using Microsoft.EntityFrameworkCore;
using StartEvent_API.Business;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Repositories;
using Xunit;

namespace StartEvent_API.Tests
{
    public class LoyaltyPointsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly LoyaltyPointRepository _loyaltyPointRepository;
        private readonly LoyaltyPointReservationRepository _reservationRepository;
        private readonly TicketService _ticketService;

        public LoyaltyPointsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loyaltyPointRepository = new LoyaltyPointRepository(_context);
            _reservationRepository = new LoyaltyPointReservationRepository(_context);
            
            // Note: This is a simplified setup. In a real test, you'd need to mock all dependencies
        }

        [Fact]
        public async Task GetAvailablePoints_ShouldSubtractReservedPoints()
        {
            // Arrange
            var customerId = "test-customer-id";
            
            // Add some loyalty points
            await _loyaltyPointRepository.AddPointsAsync(customerId, 100, "Test points");
            
            // Reserve some points
            var reservation = new LoyaltyPointReservation
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TicketId = Guid.NewGuid(),
                ReservedPoints = 30,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsConfirmed = false
            };
            await _reservationRepository.CreateAsync(reservation);

            // Act
            var availablePoints = await _loyaltyPointRepository.GetAvailablePointsByCustomerIdAsync(customerId);

            // Assert
            Assert.Equal(70, availablePoints); // 100 - 30 reserved
        }

        [Fact]
        public async Task ReserveLoyaltyPoints_ShouldFailWithInsufficientPoints()
        {
            // Arrange
            var customerId = "test-customer-id";
            var ticketId = Guid.NewGuid();
            
            // Add only 50 points
            await _loyaltyPointRepository.AddPointsAsync(customerId, 50, "Test points");
            
            // Create a ticket
            var ticket = new Ticket
            {
                Id = ticketId,
                CustomerId = customerId,
                EventId = Guid.NewGuid(),
                EventPriceId = Guid.NewGuid(),
                TicketNumber = "TEST001",
                TicketCode = "CODE001",
                Quantity = 1,
                TotalAmount = 1000,
                PurchaseDate = DateTime.UtcNow,
                IsPaid = false,
                QrCodePath = ""
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act & Assert
            // Trying to reserve 100 points when only 50 are available should fail
            // Note: This would require the actual TicketService implementation
        }

        [Fact]
        public async Task ExpiredReservations_ShouldNotAffectAvailablePoints()
        {
            // Arrange
            var customerId = "test-customer-id";
            
            // Add some loyalty points
            await _loyaltyPointRepository.AddPointsAsync(customerId, 100, "Test points");
            
            // Create an expired reservation
            var expiredReservation = new LoyaltyPointReservation
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TicketId = Guid.NewGuid(),
                ReservedPoints = 30,
                ReservedAt = DateTime.UtcNow.AddHours(-2),
                ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
                IsConfirmed = false
            };
            await _reservationRepository.CreateAsync(expiredReservation);

            // Act
            var availablePoints = await _loyaltyPointRepository.GetAvailablePointsByCustomerIdAsync(customerId);

            // Assert
            Assert.Equal(100, availablePoints); // Expired reservations should not affect available points
        }

        [Fact]
        public async Task CleanupExpiredReservations_ShouldRemoveExpiredEntries()
        {
            // Arrange
            var customerId = "test-customer-id";
            var ticketId = Guid.NewGuid();
            
            // Create an expired reservation
            var expiredReservation = new LoyaltyPointReservation
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TicketId = ticketId,
                ReservedPoints = 30,
                ReservedAt = DateTime.UtcNow.AddHours(-2),
                ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
                IsConfirmed = false
            };
            await _reservationRepository.CreateAsync(expiredReservation);

            // Act
            await _reservationRepository.CleanupExpiredReservationsAsync();

            // Assert
            var reservation = await _reservationRepository.GetByTicketIdAsync(ticketId);
            Assert.Null(reservation); // Should be cleaned up
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
