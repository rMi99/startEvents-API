using StartEvent_API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace StartEvent_API.Data.Seeders
{
    public static class EventBookingSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Skip if already seeded - check for Events instead of Venues
            if (await context.Events.AnyAsync()) return;

            // 1. GET EXISTING ASPNET ROLES (created by UserRoleSeeder)
            var roles = await context.Roles.ToListAsync();
            if (!roles.Any())
            {
                throw new InvalidOperationException("No roles found. Please ensure UserRoleSeeder runs before EventBookingSeeder.");
            }

            // Find specific roles by name
            var adminRole = roles.FirstOrDefault(r => r.Name == "Admin");
            var organizerRole = roles.FirstOrDefault(r => r.Name == "Organizer");
            var customerRole = roles.FirstOrDefault(r => r.Name == "Customer");

            if (adminRole == null || organizerRole == null || customerRole == null)
            {
                throw new InvalidOperationException("Required roles (Admin, Organizer, Customer) not found.");
            }

            // 2. GET EXISTING USERS (created by UserRoleSeeder) 
            var existingUsers = await context.Users.ToListAsync();

            // Find existing users or create new ones if needed
            var customers = existingUsers.Where(u => u.Email != null && (u.Email.Contains("customer") || u.Email.Contains("user"))).Take(2).ToList();
            var organizers = existingUsers.Where(u => u.Email != null && (u.Email.Contains("organizer") || u.Email.Contains("admin"))).Take(2).ToList();

            // If we don't have enough existing users, we'll create additional sample users
            var passwordHasher = new PasswordHasher<ApplicationUser>();

            if (customers.Count < 2)
            {
                var additionalCustomers = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "john.doe@email.com",
                        NormalizedUserName = "JOHN.DOE@EMAIL.COM",
                        Email = "john.doe@email.com",
                        NormalizedEmail = "JOHN.DOE@EMAIL.COM",
                        EmailConfirmed = true,
                        IsEmailVerified = true, // Pre-seeded users are automatically verified
                        FullName = "John Doe",
                        Address = "123 Main Street, Colombo",
                        DateOfBirth = new DateTime(1990, 5, 15),
                        PhoneNumber = "+94701234567",
                        PhoneNumberConfirmed = true,
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        LastLogin = DateTime.Now.AddDays(-1),
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    },
                    new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "jane.smith@email.com",
                        NormalizedUserName = "JANE.SMITH@EMAIL.COM",
                        Email = "jane.smith@email.com",
                        NormalizedEmail = "JANE.SMITH@EMAIL.COM",
                        EmailConfirmed = true,
                        IsEmailVerified = true, // Pre-seeded users are automatically verified
                        FullName = "Jane Smith",
                        Address = "456 Park Avenue, Kandy",
                        DateOfBirth = new DateTime(1985, 8, 22),
                        PhoneNumber = "+94707654321",
                        PhoneNumberConfirmed = true,
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        LastLogin = DateTime.Now.AddDays(-2),
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    }
                };

                // Set passwords for new customers
                foreach (var customer in additionalCustomers)
                {
                    customer.PasswordHash = passwordHasher.HashPassword(customer, "TempPassword123!");
                }

                await context.Users.AddRangeAsync(additionalCustomers);
                customers.AddRange(additionalCustomers);

                // Add user roles for new customers
                var customerRoles = additionalCustomers.Select(u => new IdentityUserRole<string>
                {
                    UserId = u.Id,
                    RoleId = customerRole.Id
                }).ToList();
                await context.UserRoles.AddRangeAsync(customerRoles);
            }

            if (organizers.Count < 2)
            {
                var additionalOrganizers = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "events@musiccompany.com",
                        NormalizedUserName = "EVENTS@MUSICCOMPANY.COM",
                        Email = "events@musiccompany.com",
                        NormalizedEmail = "EVENTS@MUSICCOMPANY.COM",
                        EmailConfirmed = true,
                        IsEmailVerified = true, // Pre-seeded users are automatically verified
                        FullName = "Music Events Manager",
                        OrganizationName = "Global Music Company",
                        OrganizationContact = "+94112345678",
                        Address = "789 Business District, Colombo 03",
                        PhoneNumber = "+94112345678",
                        PhoneNumberConfirmed = true,
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        LastLogin = DateTime.Now,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    },
                    new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "admin@techconf.com",
                        NormalizedUserName = "ADMIN@TECHCONF.COM",
                        Email = "admin@techconf.com",
                        NormalizedEmail = "ADMIN@TECHCONF.COM",
                        EmailConfirmed = true,
                        IsEmailVerified = true, // Pre-seeded users are automatically verified
                        FullName = "Tech Conference Admin",
                        OrganizationName = "Tech Innovations Ltd",
                        OrganizationContact = "+94113456789",
                        Address = "321 Tech Hub, Colombo 07",
                        PhoneNumber = "+94113456789",
                        PhoneNumberConfirmed = true,
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        LastLogin = DateTime.Now.AddHours(-3),
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    }
                };

                // Set passwords for new organizers
                foreach (var organizer in additionalOrganizers)
                {
                    organizer.PasswordHash = passwordHasher.HashPassword(organizer, "TempPassword123!");
                }

                await context.Users.AddRangeAsync(additionalOrganizers);
                organizers.AddRange(additionalOrganizers);

                // Add user roles for new organizers
                var organizerRoles = additionalOrganizers.Select(u => new IdentityUserRole<string>
                {
                    UserId = u.Id,
                    RoleId = organizerRole.Id
                }).ToList();
                await context.UserRoles.AddRangeAsync(organizerRoles);
            }

            // 3. SKIP ROLE CLAIMS, USER CLAIMS, USER LOGINS, USER TOKENS (not needed for basic seeding)

            // 4. GET EXISTING VENUES (created by VenueSeeder)
            var venues = await context.Venues.ToListAsync();
            if (!venues.Any())
            {
                throw new InvalidOperationException("No venues found. Please ensure VenueSeeder runs before EventBookingSeeder.");
            }

            // 9. EVENTS
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    VenueId = venues[0].Id,
                    OrganizerId = organizers[0].Id,
                    Title = "Music Concert",
                    Description = "Live music concert featuring famous bands",
                    EventDate = new DateTime(2025, 10, 1, 19, 0, 0),
                    EventTime = new DateTime(2025, 10, 1, 19, 0, 0),
                    Category = "Music",
                    Image = "concert.jpg",
                    IsPublished = true,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = DateTime.Now
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    VenueId = venues[1].Id,
                    OrganizerId = organizers[1].Id,
                    Title = "Tech Conference",
                    Description = "Annual tech conference with guest speakers",
                    EventDate = new DateTime(2025, 11, 15, 9, 0, 0),
                    EventTime = new DateTime(2025, 11, 15, 9, 0, 0),
                    Category = "Conference",
                    Image = "tech.jpg",
                    IsPublished = true,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = DateTime.Now
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    VenueId = venues[0].Id,
                    OrganizerId = organizers[0].Id,
                    Title = "Art Exhibition",
                    Description = "Contemporary art exhibition featuring local artists",
                    EventDate = new DateTime(2025, 12, 5, 10, 0, 0),
                    EventTime = new DateTime(2025, 12, 5, 10, 0, 0),
                    Category = "Art",
                    Image = "art-exhibition.jpg",
                    IsPublished = true,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = DateTime.Now
                }
            };
            await context.Events.AddRangeAsync(events);

            // 10. EVENT PRICES
            var eventPrices = new List<EventPrice>
            {
                // Music Concert prices (High-end event)
                new EventPrice { Id = Guid.NewGuid(), EventId = events[0].Id, Category = "VIP", Stock = 50, IsActive = true, Price = 7500m },
                new EventPrice { Id = Guid.NewGuid(), EventId = events[0].Id, Category = "General", Stock = 200, IsActive = true, Price = 2500m },
                new EventPrice { Id = Guid.NewGuid(), EventId = events[0].Id, Category = "Student", Stock = 100, IsActive = true, Price = 2000m },

                // Tech Conference prices (Executive-level access)
                new EventPrice { Id = Guid.NewGuid(), EventId = events[1].Id, Category = "Standard", Stock = 300, IsActive = true, Price = 3000m },
                new EventPrice { Id = Guid.NewGuid(), EventId = events[1].Id, Category = "Premium", Stock = 50, IsActive = true, Price = 10000m },

                // Art Exhibition prices (Luxury private viewing)
                new EventPrice { Id = Guid.NewGuid(), EventId = events[2].Id, Category = "Adult", Stock = 150, IsActive = true, Price = 2500m },
                new EventPrice { Id = Guid.NewGuid(), EventId = events[2].Id, Category = "Student", Stock = 100, IsActive = true, Price = 2000m }
            };

            await context.EventPrices.AddRangeAsync(eventPrices);

            // 11. DISCOUNTS
            var discounts = new List<Discount>
            {
                new Discount { Id = Guid.NewGuid(), EventId = events[0].Id, Code = "VIP10", Type = "Percentage", Value = 10m, ValidFrom = new DateTime(2025, 9, 1), ValidTo = new DateTime(2025, 10, 1, 23, 59, 59), IsActive = true },
                new Discount { Id = Guid.NewGuid(), EventId = events[1].Id, Code = "CONF50", Type = "Fixed", Value = 50m, ValidFrom = new DateTime(2025, 9, 1), ValidTo = new DateTime(2025, 11, 15, 23, 59, 59), IsActive = true },
                new Discount { Id = Guid.NewGuid(), EventId = events[0].Id, Code = "MUSIC20", Type = "Percentage", Value = 20m, ValidFrom = new DateTime(2025, 9, 15), ValidTo = new DateTime(2025, 9, 30, 23, 59, 59), IsActive = true },
                new Discount { Id = Guid.NewGuid(), EventId = events[2].Id, Code = "ART15", Type = "Percentage", Value = 15m, ValidFrom = new DateTime(2025, 11, 1), ValidTo = new DateTime(2025, 12, 5, 23, 59, 59), IsActive = true },
                new Discount { Id = Guid.NewGuid(), EventId = null, Code = "WELCOME", Type = "Fixed", Value = 25m, ValidFrom = new DateTime(2025, 9, 1), ValidTo = new DateTime(2025, 12, 31, 23, 59, 59), IsActive = true } // Global discount
            };
            await context.Discounts.AddRangeAsync(discounts);

            // 12. TICKETS with QR codes
            var tickets = new List<Ticket>
            {
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[0].Id,
                    EventId = events[0].Id,
                    EventPriceId = eventPrices[1].Id, // General ticket
                    TicketNumber = "TN001",
                    TicketCode = "TCODE001",
                    Quantity = 2,
                    TotalAmount = 100m,
                    PurchaseDate = DateTime.Now,
                    IsPaid = true,
                    QrCodePath = GenerateQrCodeBase64("TCODE001")
                },
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[1].Id,
                    EventId = events[1].Id,
                    EventPriceId = eventPrices[3].Id, // Standard conference ticket
                    TicketNumber = "TN002",
                    TicketCode = "TCODE002",
                    Quantity = 1,
                    TotalAmount = 100m,
                    PurchaseDate = DateTime.Now,
                    IsPaid = false,
                    QrCodePath = GenerateQrCodeBase64("TCODE002")
                },
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[0].Id,
                    EventId = events[0].Id,
                    EventPriceId = eventPrices[0].Id, // VIP ticket
                    TicketNumber = "TN003",
                    TicketCode = "TCODE003",
                    Quantity = 1,
                    TotalAmount = 135m, // 150 - 10% VIP10 discount
                    PurchaseDate = DateTime.Now.AddDays(-1),
                    IsPaid = true,
                    QrCodePath = GenerateQrCodeBase64("TCODE003")
                },
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[1].Id,
                    EventId = events[2].Id,
                    EventPriceId = eventPrices[5].Id, // Adult art exhibition ticket
                    TicketNumber = "TN004",
                    TicketCode = "TCODE004",
                    Quantity = 2,
                    TotalAmount = 60m,
                    PurchaseDate = DateTime.Now.AddDays(-3),
                    IsPaid = true,
                    QrCodePath = GenerateQrCodeBase64("TCODE004")
                }
            };
            await context.Tickets.AddRangeAsync(tickets);

            // 13. PAYMENTS
            var payments = new List<Payment>
            {
                new Payment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = tickets[0].CustomerId,
                    TicketId = tickets[0].Id,
                    Amount = tickets[0].TotalAmount,
                    PaymentDate = DateTime.Now,
                    Status = "Completed",
                    PaymentMethod = "Credit Card",
                    TransactionId = "TXN001"
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = tickets[1].CustomerId,
                    TicketId = tickets[1].Id,
                    Amount = tickets[1].TotalAmount,
                    PaymentDate = DateTime.Now,
                    Status = "Pending",
                    PaymentMethod = "PayPal",
                    TransactionId = "TXN002"
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = tickets[2].CustomerId,
                    TicketId = tickets[2].Id,
                    Amount = tickets[2].TotalAmount,
                    PaymentDate = DateTime.Now.AddDays(-1),
                    Status = "Completed",
                    PaymentMethod = "Debit Card",
                    TransactionId = "TXN003"
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = tickets[3].CustomerId,
                    TicketId = tickets[3].Id,
                    Amount = tickets[3].TotalAmount,
                    PaymentDate = DateTime.Now.AddDays(-3),
                    Status = "Completed",
                    PaymentMethod = "Bank Transfer",
                    TransactionId = "TXN004"
                }
            };
            await context.Payments.AddRangeAsync(payments);

            // 14. LOYALTY POINTS
            var loyaltyPoints = new List<LoyaltyPoint>
            {
                new LoyaltyPoint
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[0].Id,
                    Points = 50,
                    EarnedDate = DateTime.Now,
                    Description = "Purchased tickets for Music Concert"
                },
                new LoyaltyPoint
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[1].Id,
                    Points = 20,
                    EarnedDate = DateTime.Now,
                    Description = "Purchased tickets for Tech Conference"
                },
                new LoyaltyPoint
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[0].Id,
                    Points = 75,
                    EarnedDate = DateTime.Now.AddDays(-1),
                    Description = "Purchased VIP tickets for Music Concert"
                },
                new LoyaltyPoint
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[1].Id,
                    Points = 30,
                    EarnedDate = DateTime.Now.AddDays(-3),
                    Description = "Purchased tickets for Art Exhibition"
                },
                new LoyaltyPoint
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[0].Id,
                    Points = 10,
                    EarnedDate = DateTime.Now.AddDays(-7),
                    Description = "Account registration bonus"
                },
                new LoyaltyPoint
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customers[1].Id,
                    Points = 10,
                    EarnedDate = DateTime.Now.AddDays(-10),
                    Description = "Account registration bonus"
                }
            };
            await context.LoyaltyPoints.AddRangeAsync(loyaltyPoints);

            // Save all changes
            await context.SaveChangesAsync();
            Console.WriteLine("Event booking data seeded successfully.");
            Console.WriteLine($"Seeded: {roles.Count} roles (existing), {customers.Count + organizers.Count} users, {venues.Count} venues (existing)");
            Console.WriteLine($"Seeded: {events.Count} events, {eventPrices.Count} event prices, {discounts.Count} discounts, {tickets.Count} tickets, {payments.Count} payments, {loyaltyPoints.Count} loyalty points");
        }

        // QR code generator helper
        private static string GenerateQrCodeBase64(string code)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new BitmapByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            var base64 = Convert.ToBase64String(qrCodeBytes);
            return $"data:image/png;base64,{base64}";
        }
    }
}
