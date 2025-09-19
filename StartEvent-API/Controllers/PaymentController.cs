using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Data.Entities;
using StartEvent_API.Repositories;
using StartEvent_API.Business;
using StartEvent_API.Models.Email;
using StartEvent_API.Services.Email;
using System.Security.Claims;
using Stripe;
using Stripe.Checkout;
using Microsoft.EntityFrameworkCore;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    // ... existing code ...
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IQrService _qrService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentRepository paymentRepository,
            ITicketRepository ticketRepository,
            IQrService qrService,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<PaymentController> logger)
        {
            _paymentRepository = paymentRepository;
            _ticketRepository = ticketRepository;
            _qrService = qrService;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        /// <summary>
        /// Force QR generation for a paid ticket (useful for fixing missing QR codes)
        /// </summary>
        [HttpPost("force-qr-generation")]
        [Authorize(Roles = "Customer,Admin,Organizer")]
        public async Task<IActionResult> ForceQrGeneration([FromBody] MarkPaidRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated" });

                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId);
                if (ticket == null)
                    return NotFound(new { Message = "Ticket not found" });

                // Customers can only generate QR for their own ticket; Admin/Organizer can override
                var userIsPrivileged = User.IsInRole("Admin") || User.IsInRole("Organizer");
                if (!userIsPrivileged && ticket.CustomerId != userId)
                    return StatusCode(403, new { Message = "Access denied" });

                if (!ticket.IsPaid)
                    return BadRequest(new { Message = "Ticket must be paid before generating QR code" });

                var qrResult = await _qrService.GenerateQrCodeAsync(ticket.Id, ticket.CustomerId);

                if (qrResult.Success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "QR code generated successfully",
                        TicketId = ticket.Id,
                        TicketCode = qrResult.TicketCode,
                        QrCodePath = qrResult.QrCodePath
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = qrResult.Message,
                        Errors = qrResult.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Failed to generate QR code", Error = ex.Message });
            }
        }

        /// <summary>
        /// Manually mark a ticket as paid and trigger QR generation
        /// </summary>
        [HttpPost("mark-paid")]
        [Authorize(Roles = "Customer,Admin,Organizer")]
        public async Task<IActionResult> MarkPaid([FromBody] MarkPaidRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated" });

                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId);
                if (ticket == null)
                    return NotFound(new { Message = "Ticket not found" });

                // Customers can only mark their own ticket; Admin/Organizer can override
                var userIsPrivileged = User.IsInRole("Admin") || User.IsInRole("Organizer");
                if (!userIsPrivileged && ticket.CustomerId != userId)
                    return StatusCode(403, new { Message = "Access denied" });

                if (!ticket.IsPaid)
                {
                    ticket.IsPaid = true;
                    await _ticketRepository.UpdateAsync(ticket);

                    // Create a payment record if one does not already exist
                    var payments = await _paymentRepository.GetByTicketIdAsync(ticket.Id);
                    if (!payments.Any())
                    {
                        var payment = new Data.Entities.Payment
                        {
                            Id = Guid.NewGuid(),
                            CustomerId = ticket.CustomerId,
                            TicketId = ticket.Id,
                            Amount = ticket.TotalAmount,
                            PaymentDate = DateTime.UtcNow,
                            Status = "Completed",
                            PaymentMethod = "Manual",
                            TransactionId = null
                        };
                        await _paymentRepository.CreateAsync(payment);
                    }

                    try
                    {
                        await _qrService.GenerateQrCodeAsync(ticket.Id, ticket.CustomerId);
                    }
                    catch (Exception ex)
                    {
                        // QR generation failure should not fail status update
                        Console.WriteLine($"QR generation failed in MarkPaid: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    Success = true,
                    TicketId = ticket.Id,
                    IsPaid = ticket.IsPaid
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Failed to mark ticket as paid", Error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a Stripe Checkout session for a ticket
        /// </summary>
        [HttpPost("create-checkout-session")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId);
            if (ticket == null || ticket.Event == null)
            {
                return NotFound(new { Message = "Ticket or associated event not found" });
            }

            // Your frontend domain
            var domain = _configuration["FrontendDomain"] ?? "http://localhost:3000";

            // Use the values from the request : ticket.TotalAmount
            var unitAmount = request.UnitPrice; // Already in cents from frontend
            var quantity = request.Quantity;
            var ticketType = request.TicketType ?? ticket.Event.Title;

            _logger.LogInformation("Creating Stripe checkout session: UnitPrice={UnitPrice} cents, Quantity={Quantity}, Total={Total} cents", 
                unitAmount, quantity, unitAmount * quantity);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = unitAmount, // Use the unitPrice from frontend (already in cents)
                            Currency = "lkr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = ticketType,
                                Description = $"{quantity} ticket(s) for {ticket.Event.Title}",
                            },
                        },
                        Quantity = quantity, // Use quantity from frontend
                    },
                },
                Mode = "payment",
                // Stripe will redirect to these URLs after payment attempt
                SuccessUrl = $"{domain}/booking-confirmation?success=true&session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/events/{ticket.EventId}/booking?canceled=true",
                // We pass the ticketId to identify the order in our webhook
                ClientReferenceId = ticket.Id.ToString()
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            _logger.LogInformation("Stripe checkout session created: {SessionId}, Total amount: {Total} cents", 
                session.Id, unitAmount * quantity);

            return Ok(new { url = session.Url });
        }

        /// <summary>
        /// Creates a Stripe Payment Intent for direct payment processing
        /// </summary>
        [HttpPost("create-payment-intent")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated" });

            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId);
            if (ticket == null)
                return NotFound(new { Message = "Ticket not found" });

            // Ensure user owns this ticket
            if (ticket.CustomerId != userId)
                return StatusCode(403, new { Message = "Access denied" });

            // Check if already paid
            if (ticket.IsPaid)
                return BadRequest(new { Message = "Ticket is already paid" });

            try
            {
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = (long)(ticket.TotalAmount * 100), // Amount in cents
                    Currency = "lkr",
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "ticketId", ticket.Id.ToString() },
                        { "customerId", userId }
                    }
                });

                return Ok(new
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    Amount = ticket.TotalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to create payment intent", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get payment status for a ticket
        /// </summary>
        [HttpGet("status/{ticketId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetPaymentStatus(Guid ticketId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated" });

            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
                return NotFound(new { Message = "Ticket not found" });

            // Ensure user owns this ticket
            if (ticket.CustomerId != userId)
                return StatusCode(403, new { Message = "Access denied" });

            var payments = await _paymentRepository.GetByTicketIdAsync(ticketId);
            var payment = payments.FirstOrDefault();

            return Ok(new
            {
                TicketId = ticketId,
                IsPaid = ticket.IsPaid,
                PaymentStatus = payment?.Status ?? "Pending",
                PaymentDate = payment?.PaymentDate,
                Amount = ticket.TotalAmount,
                HasQrCode = !string.IsNullOrEmpty(ticket.QrCodePath),
                QrCodePath = ticket.QrCodePath // Add this line
            });
        }

        /// <summary>
        /// Get session status for payment confirmation
        /// </summary>
        [HttpGet("session-status/{sessionId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetSessionStatus(string sessionId)
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);

                return Ok(new
                {
                    Status = session.PaymentStatus,
                    CustomerEmail = session.CustomerDetails?.Email,
                    TicketId = session.ClientReferenceId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Invalid session ID", Error = ex.Message });
            }
        }



        /// <summary>
        /// Webhook endpoint for Stripe payment confirmations
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeEvent = EventUtility.ConstructEvent(json,
                Request.Headers["Stripe-Signature"],
                _configuration["Stripe:WebhookSecret"]);

            // Handle the checkout.session.completed event
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session?.ClientReferenceId != null)
                {
                    var ticketId = Guid.Parse(session.ClientReferenceId);
                    await ProcessSuccessfulPayment(ticketId, session.PaymentIntentId);
                }
            }
            // Handle payment_intent.succeeded event for direct payments
            else if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent?.Metadata?.ContainsKey("ticketId") == true)
                {
                    var ticketId = Guid.Parse(paymentIntent.Metadata["ticketId"]);
                    await ProcessSuccessfulPayment(ticketId, paymentIntent.Id);
                }
            }

            return Ok();
        }

        /// <summary>
        /// Private method to handle successful payment processing
        /// </summary>
        private async Task ProcessSuccessfulPayment(Guid ticketId, string? transactionId)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket != null && !ticket.IsPaid)
            {
                // Mark ticket as paid
                ticket.IsPaid = true;
                await _ticketRepository.UpdateAsync(ticket);

                // Create payment record
                var payment = new Data.Entities.Payment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = ticket.CustomerId,
                    TicketId = ticket.Id,
                    Amount = ticket.TotalAmount,
                    PaymentDate = DateTime.UtcNow,
                    Status = "Completed",
                    PaymentMethod = "Card",
                    TransactionId = transactionId
                };
                await _paymentRepository.CreateAsync(payment);

                // Send payment and ticket confirmation emails
                await SendTicketConfirmationEmailAsync(ticket, payment);

                // Generate QR code automatically after successful payment
                try
                {
                    var qrResult = await _qrService.GenerateQrCodeAsync(ticket.Id, ticket.CustomerId);
                    if (!qrResult.Success)
                    {
                        Console.WriteLine($"QR generation failed for ticket {ticketId}: {qrResult.Message}");
                        if (qrResult.Errors != null)
                        {
                            foreach (var error in qrResult.Errors)
                            {
                                Console.WriteLine($"QR Error: {error}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"QR code generated successfully for ticket {ticketId}, code: {qrResult.TicketCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the webhook
                    Console.WriteLine($"Exception during QR generation for ticket {ticketId}: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Sends ticket confirmation and payment confirmation emails
        /// </summary>
        private async Task SendTicketConfirmationEmailAsync(Ticket ticket, Data.Entities.Payment payment)
        {
            try
            {
                // Get ticket with related data for email
                var ticketWithDetails = await _ticketRepository.GetByIdAsync(ticket.Id);
                if (ticketWithDetails == null) return;

                // Get customer information (assuming you have a way to get user details)
                // This is a simplified version - you might need to inject UserManager or create a user repository
                var customerEmail = ""; // You'll need to get this from your user system
                var customerName = ""; // You'll need to get this from your user system

                // For now, let's create a basic email - you should enhance this with proper user data
                var ticketConfirmationEmail = new TicketConfirmationEmailTemplate
                {
                    To = new EmailRecipient
                    {
                        Email = customerEmail,
                        Name = customerName
                    },
                    Ticket = ticket,
                    Event = new StartEvent_API.Data.Entities.Event(), // You'll need to load the actual event
                    Venue = new Venue(), // You'll need to load the actual venue
                    Subject = $"Your Ticket Confirmation - Order #{ticket.TicketNumber}"
                };

                // Send ticket confirmation email
                var ticketResult = await _emailService.SendTicketConfirmationEmailAsync(ticketConfirmationEmail);
                if (ticketResult.Success)
                {
                    _logger.LogInformation("Ticket confirmation email sent for ticket {TicketId}", ticket.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to send ticket confirmation email for ticket {TicketId}: {Error}",
                        ticket.Id, ticketResult.ErrorMessage);
                }

                // Send payment confirmation email
                var paymentConfirmationEmail = new PaymentConfirmationEmailTemplate
                {
                    To = new EmailRecipient
                    {
                        Email = customerEmail,
                        Name = customerName
                    },
                    Payment = payment,
                    Ticket = ticket,
                    Event = new StartEvent_API.Data.Entities.Event(), // You'll need to load the actual event
                    Subject = $"Payment Confirmation - Transaction #{payment.TransactionId}"
                };

                var paymentResult = await _emailService.SendPaymentConfirmationEmailAsync(paymentConfirmationEmail);
                if (paymentResult.Success)
                {
                    _logger.LogInformation("Payment confirmation email sent for payment {PaymentId}", payment.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to send payment confirmation email for payment {PaymentId}: {Error}",
                        payment.Id, paymentResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending confirmation emails for ticket {TicketId}", ticket.Id);
            }
        }
    }

    // Request models
    public class CreateCheckoutSessionRequest
    {
        public Guid TicketId { get; set; }
        public string? TicketType { get; set; }
        public long UnitPrice { get; set; } // Price in cents
        public int Quantity { get; set; }
    }

    public class CreatePaymentIntentRequest
    {
        public Guid TicketId { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public Guid TicketId { get; set; }
        public string PaymentMethod { get; set; } = "Card";
        public string? StripeToken { get; set; } // For Stripe integration
    }

    public class MarkPaidRequest
    {
        public Guid TicketId { get; set; }
    }
}
