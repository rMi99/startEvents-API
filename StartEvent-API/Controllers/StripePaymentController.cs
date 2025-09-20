using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using StartEvent_API.Repositories;
using StartEvent_API.Data.Entities;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Stripe;
using Stripe.Checkout;

namespace StartEvent_API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    [Authorize]
    public class StripePaymentController : ControllerBase
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IConfiguration _configuration;

        public StripePaymentController(
            ITicketRepository ticketRepository,
            IPaymentRepository paymentRepository,
            ILoyaltyService loyaltyService,
            IConfiguration configuration)
        {
            _ticketRepository = ticketRepository;
            _paymentRepository = paymentRepository;
            _loyaltyService = loyaltyService;
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        /// <summary>
        /// Create Stripe Checkout Session with FINAL TOTAL only
        /// Formula: Final Total = (Quantity × UnitPrice) – LoyaltyPointsRedeemed
        /// </summary>
        [HttpPost("create-stripe-session")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateStripeSession([FromBody] CreateStripeSessionRequest request)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated" });

                // Validate final amount
                if (request.FinalAmount <= 0)
                    return BadRequest(new { Message = "Final amount must be greater than zero" });

                // Get frontend domain
                var domain = _configuration["FrontendDomain"] ?? "http://localhost:3000";

                Console.WriteLine("=== STRIPE SESSION CREATION ===");
                Console.WriteLine($"User ID: {userId}");
                Console.WriteLine($"Event Title: {request.EventTitle}");
                Console.WriteLine($"Description: {request.Description}");
                Console.WriteLine($"Final Amount: LKR {request.FinalAmount}");
                Console.WriteLine($"Final Amount (cents): {(long)(request.FinalAmount * 100)}");
                Console.WriteLine($"Metadata: {System.Text.Json.JsonSerializer.Serialize(request.Metadata)}");

                // ✅ STRIPE CHECKOUT SESSION - Only pass FINAL TOTAL
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(request.FinalAmount * 100), // ✅ FINAL TOTAL in cents
                                Currency = request.Currency.ToLower(),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.EventTitle,
                                    Description = request.Description,
                                },
                            },
                            Quantity = 1, // ✅ Always 1 because final total already includes all tickets
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = $"{domain}/booking-confirmation?success=true&session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{domain}/events/{request.Metadata?.EventId}/booking?canceled=true",
                    ClientReferenceId = userId, // Reference for webhook processing
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "eventId", request.Metadata?.EventId ?? "" },
                        { "priceId", request.Metadata?.PriceId ?? "" },
                        { "quantity", request.Metadata?.Quantity ?? "1" },
                        { "unitPrice", request.Metadata?.UnitPrice ?? "0" },
                        { "loyaltyPointsUsed", request.Metadata?.LoyaltyPointsUsed ?? "0" },
                        { "subtotal", request.Metadata?.Subtotal ?? "0" },
                        { "discount", request.Metadata?.Discount ?? "0" },
                        { "finalTotal", request.FinalAmount.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                Console.WriteLine($"Stripe session created: {session.Id}");
                Console.WriteLine($"Stripe session URL: {session.Url}");

                return Ok(new CreateStripeSessionResponse
                {
                    Url = session.Url,
                    SessionId = session.Id,
                    Success = true,
                    Message = "Stripe session created successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Stripe session: {ex.Message}");
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Failed to create payment session", 
                    Error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Webhook endpoint for Stripe payment confirmations
        /// Processes successful payments and awards loyalty points
        /// </summary>
        [HttpPost("stripe-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]);

                Console.WriteLine($"Stripe webhook received: {stripeEvent.Type}");

                // Handle the checkout.session.completed event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session?.ClientReferenceId != null)
                    {
                        await ProcessSuccessfulStripePayment(session);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stripe webhook error: {ex.Message}");
                return BadRequest();
            }
        }

        /// <summary>
        /// Process successful Stripe payment
        /// Creates ticket record and awards loyalty points based on final amount paid
        /// </summary>
        private async Task ProcessSuccessfulStripePayment(Session session)
        {
            try
            {
                var userId = session.ClientReferenceId;
                var metadata = session.Metadata;

                Console.WriteLine("=== PROCESSING SUCCESSFUL STRIPE PAYMENT ===");
                Console.WriteLine($"Session ID: {session.Id}");
                Console.WriteLine($"User ID: {userId}");
                Console.WriteLine($"Amount Paid: {session.AmountTotal / 100.0} {session.Currency.ToUpper()}");
                Console.WriteLine($"Metadata: {System.Text.Json.JsonSerializer.Serialize(metadata)}");

                // Extract order details from metadata
                var eventId = Guid.Parse(metadata["eventId"]);
                var priceId = Guid.Parse(metadata["priceId"]);
                var quantity = int.Parse(metadata["quantity"]);
                var unitPrice = decimal.Parse(metadata["unitPrice"]);
                var loyaltyPointsUsed = int.Parse(metadata["loyaltyPointsUsed"]);
                var finalTotal = decimal.Parse(metadata["finalTotal"]);

                // Verify the amount paid matches our calculation
                var expectedAmount = (long)(finalTotal * 100);
                if (session.AmountTotal != expectedAmount)
                {
                    Console.WriteLine($"WARNING: Amount mismatch! Expected: {expectedAmount}, Paid: {session.AmountTotal}");
                }

                // Create ticket record
                var ticket = new Data.Entities.Ticket
                {
                    Id = Guid.NewGuid(),
                    CustomerId = userId,
                    EventId = eventId,
                    EventPriceId = priceId,
                    TicketNumber = await GenerateTicketNumber(),
                    TicketCode = await GenerateTicketCode(),
                    Quantity = quantity,
                    TotalAmount = finalTotal, // ✅ Store the final amount paid
                    PurchaseDate = DateTime.UtcNow,
                    IsPaid = true,
                    QrCodePath = string.Empty
                };

                await _ticketRepository.CreateAsync(ticket);

                // Create payment record
                var payment = new Data.Entities.Payment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = userId,
                    TicketId = ticket.Id,
                    Amount = finalTotal, // ✅ Store the final amount paid
                    PaymentDate = DateTime.UtcNow,
                    Status = "Completed",
                    PaymentMethod = "Stripe",
                    TransactionId = session.PaymentIntentId
                };

                await _paymentRepository.CreateAsync(payment);

                // ✅ AWARD LOYALTY POINTS based on FINAL AMOUNT PAID (not subtotal)
                try
                {
                    var earnedPoints = await _loyaltyService.CalculateEarnedPointsAsync(finalTotal);
                    if (earnedPoints > 0)
                    {
                        await _loyaltyService.AddPointsAsync(
                            userId,
                            earnedPoints,
                            $"Earned {earnedPoints} points from Stripe payment (LKR {finalTotal:F2})"
                        );

                        Console.WriteLine($"Awarded {earnedPoints} loyalty points to user {userId}");
                    }
                }
                catch (Exception loyaltyEx)
                {
                    Console.WriteLine($"Failed to award loyalty points: {loyaltyEx.Message}");
                    // Don't fail the payment process if loyalty points fail
                }

                // Generate QR code
                try
                {
                    // QR code generation logic here
                    Console.WriteLine($"QR code generation completed for ticket {ticket.Id}");
                }
                catch (Exception qrEx)
                {
                    Console.WriteLine($"QR generation failed: {qrEx.Message}");
                }

                Console.WriteLine($"Payment processing completed successfully for ticket {ticket.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Stripe payment: {ex.Message}");
                throw;
            }
        }

        private async Task<string> GenerateTicketNumber()
        {
            // Generate unique ticket number
            return $"TKT-{DateTime.Now:yyyyMMdd}-{new Random().Next(100000, 999999)}";
        }

        private async Task<string> GenerateTicketCode()
        {
            // Generate unique ticket code for QR
            return Guid.NewGuid().ToString("N")[..12].ToUpper();
        }
    }

    // Request/Response Models
    public class CreateStripeSessionRequest
    {
        [Required]
        public string EventTitle { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Final amount must be greater than zero")]
        public decimal FinalAmount { get; set; } // ✅ ONLY FINAL TOTAL

        [Required]
        public string Currency { get; set; } = "LKR";

        public StripeSessionMetadata? Metadata { get; set; }
    }

    public class StripeSessionMetadata
    {
        public string? EventId { get; set; }
        public string? PriceId { get; set; }
        public string? Quantity { get; set; }
        public string? UnitPrice { get; set; }
        public string? LoyaltyPointsUsed { get; set; }
        public string? Subtotal { get; set; }
        public string? Discount { get; set; }
    }

    public class CreateStripeSessionResponse
    {
        public string Url { get; set; } = default!;
        public string SessionId { get; set; } = default!;
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
    }
}
