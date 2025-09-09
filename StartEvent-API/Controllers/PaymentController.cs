using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Data.Entities;
using StartEvent_API.Repositories;
using System.Security.Claims;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITicketRepository _ticketRepository;

        public PaymentController(IPaymentRepository paymentRepository, ITicketRepository ticketRepository)
        {
            _paymentRepository = paymentRepository;
            _ticketRepository = ticketRepository;
        }

        /// <summary>
        /// Process payment for a ticket via Stripe
        /// </summary>
        [HttpPost("process")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(customerId))
                    return Unauthorized("User not authenticated");

                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId);
                if (ticket == null)
                    return NotFound(new { Success = false, Message = "Ticket not found" });

                if (ticket.CustomerId != customerId)
                    return Forbid("You can only pay for your own tickets");

                if (ticket.IsPaid)
                    return BadRequest(new { Success = false, Message = "Ticket is already paid" });

                // TODO: Integrate with Stripe API
                // For now, we'll simulate a successful payment
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    TicketId = request.TicketId,
                    Amount = ticket.TotalAmount,
                    PaymentDate = DateTime.UtcNow,
                    Status = "Completed", // In real implementation, this would come from Stripe
                    PaymentMethod = request.PaymentMethod,
                    TransactionId = $"stripe_{Guid.NewGuid()}" // In real implementation, this would be Stripe's transaction ID
                };

                await _paymentRepository.CreateAsync(payment);

                // Update ticket as paid
                ticket.IsPaid = true;
                await _ticketRepository.UpdateAsync(ticket);

                // TODO: Send confirmation email via Brevo
                // await _emailService.SendTicketConfirmationAsync(ticket, payment);

                return Ok(new
                {
                    Success = true,
                    Message = "Payment processed successfully",
                    Data = new
                    {
                        PaymentId = payment.Id,
                        TransactionId = payment.TransactionId,
                        Amount = payment.Amount,
                        Status = payment.Status,
                        TicketNumber = ticket.TicketNumber
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing payment" });
            }
        }

        /// <summary>
        /// Get payment history for a customer
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(customerId))
                    return Unauthorized("User not authenticated");

                var payments = await _paymentRepository.GetByCustomerIdAsync(customerId);

                return Ok(new
                {
                    Success = true,
                    Data = payments.Select(p => new
                    {
                        p.Id,
                        p.Amount,
                        p.PaymentDate,
                        p.Status,
                        p.PaymentMethod,
                        p.TransactionId,
                        TicketNumber = p.Ticket.TicketNumber,
                        EventTitle = p.Ticket.Event.Title
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving payment history" });
            }
        }

        /// <summary>
        /// Get payment details by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var payment = await _paymentRepository.GetByIdAsync(id);
                if (payment == null)
                    return NotFound(new { Success = false, Message = "Payment not found" });

                // Customers can only view their own payments, Admins can view any
                if (userRole != "Admin" && payment.CustomerId != customerId)
                    return Forbid("You can only view your own payments");

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        payment.Id,
                        payment.Amount,
                        payment.PaymentDate,
                        payment.Status,
                        payment.PaymentMethod,
                        payment.TransactionId,
                        Customer = new
                        {
                            payment.Customer.Id,
                            payment.Customer.Email,
                            payment.Customer.FullName
                        },
                        Ticket = new
                        {
                            payment.Ticket.Id,
                            payment.Ticket.TicketNumber,
                            payment.Ticket.TicketCode,
                            EventTitle = payment.Ticket.Event.Title
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving payment details" });
            }
        }

        /// <summary>
        /// Webhook endpoint for Stripe payment confirmations
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                // TODO: Implement Stripe webhook verification and processing
                // This would handle payment confirmations, failures, and refunds from Stripe

                return Ok(new { Success = true, Message = "Webhook received" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Webhook processing failed" });
            }
        }
    }

    // Request models
    public class ProcessPaymentRequest
    {
        public Guid TicketId { get; set; }
        public string PaymentMethod { get; set; } = "Card";
        public string? StripeToken { get; set; } // For Stripe integration
    }
}
