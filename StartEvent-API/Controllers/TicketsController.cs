using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models;
using System.Security.Claims;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(
            ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        /// <summary>
        /// Book tickets for an event
        /// </summary>
        [HttpPost("book")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> BookTicket([FromBody] BookTicketRequest request)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(customerId))
                    return Unauthorized("User not authenticated");

                var ticket = await _ticketService.BookTicketAsync(
                    customerId, 
                    request.EventId, 
                    request.EventPriceId, 
                    request.Quantity, 
                    request.DiscountCode, 
                    request.UseLoyaltyPoints);

                var ticketDto = new TicketDto
                {
                    Id = ticket.Id,
                    CustomerId = ticket.CustomerId,
                    EventId = ticket.EventId,
                    EventPriceId = ticket.EventPriceId,
                    TicketNumber = ticket.TicketNumber,
                    TicketCode = ticket.TicketCode,
                    Quantity = ticket.Quantity,
                    TotalAmount = ticket.TotalAmount,
                    PurchaseDate = ticket.PurchaseDate,
                    IsPaid = ticket.IsPaid,
                    QrCodePath = ticket.QrCodePath
                };

                return Ok(new { 
                    Success = true, 
                    Message = "Ticket booked successfully", 
                    Data = ticketDto 
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while booking the ticket", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get ticket details by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetTicket(Guid id)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var ticket = await _ticketService.GetTicketByIdAsync(id);

                if (ticket == null)
                    return NotFound(new { Success = false, Message = "Ticket not found" });

                // Ensure customer can only view their own tickets
                if (ticket.CustomerId != customerId)
                    return Forbid("You can only view your own tickets");

                var ticketDto = new TicketDto
                {
                    Id = ticket.Id,
                    CustomerId = ticket.CustomerId,
                    EventId = ticket.EventId,
                    EventPriceId = ticket.EventPriceId,
                    TicketNumber = ticket.TicketNumber,
                    TicketCode = ticket.TicketCode,
                    Quantity = ticket.Quantity,
                    TotalAmount = ticket.TotalAmount,
                    PurchaseDate = ticket.PurchaseDate,
                    IsPaid = ticket.IsPaid,
                    QrCodePath = ticket.QrCodePath
                };

                return Ok(new { Success = true, Data = ticketDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving the ticket", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get customer's ticket history with pagination
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetTicketHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(customerId))
                    return Unauthorized("User not authenticated");

                var tickets = await _ticketService.GetCustomerTicketsAsync(customerId, page, pageSize);

                return Ok(new { 
                    Success = true, 
                    Data = tickets,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving ticket history", Error = ex.Message });
            }
        }

        /// <summary>
        /// Apply promotional code to a ticket
        /// </summary>
        [HttpPost("promotions")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionRequest request)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var ticket = await _ticketService.GetTicketByIdAsync(request.TicketId);

                if (ticket == null)
                    return NotFound(new { Success = false, Message = "Ticket not found" });

                if (ticket.CustomerId != customerId)
                    return Forbid("You can only modify your own tickets");

                var success = await _ticketService.ApplyPromotionAsync(request.TicketId, request.DiscountCode);

                if (!success)
                    return BadRequest(new { Success = false, Message = "Invalid or expired discount code" });

                return Ok(new { Success = true, Message = "Promotion applied successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while applying promotion", Error = ex.Message });
            }
        }

        /// <summary>
        /// Use loyalty points for a ticket
        /// </summary>
        [HttpPost("loyalty-points")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UseLoyaltyPoints([FromBody] UseLoyaltyPointsRequest request)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var ticket = await _ticketService.GetTicketByIdAsync(request.TicketId);

                if (ticket == null)
                    return NotFound(new { Success = false, Message = "Ticket not found" });

                if (ticket.CustomerId != customerId)
                    return Forbid("You can only modify your own tickets");

                var success = await _ticketService.UseLoyaltyPointsAsync(request.TicketId, request.Points);

                if (!success)
                    return BadRequest(new { Success = false, Message = "Insufficient loyalty points or invalid ticket" });

                return Ok(new { Success = true, Message = "Loyalty points applied successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while applying loyalty points", Error = ex.Message });
            }
        }

        /// <summary>
        /// Generate QR code for a ticket
        /// </summary>
        [HttpGet("{id}/qrcode")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GenerateQRCode(Guid id)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var ticket = await _ticketService.GetTicketByIdAsync(id);

                if (ticket == null)
                    return NotFound(new { Success = false, Message = "Ticket not found" });

                if (ticket.CustomerId != customerId)
                    return Forbid("You can only generate QR codes for your own tickets");

                var qrCodePath = await _ticketService.GenerateQRCodeAsync(id);

                return Ok(new { 
                    Success = true, 
                    QRCodePath = qrCodePath,
                    TicketCode = ticket.TicketCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating QR code", Error = ex.Message });
            }
        }

        /// <summary>
        /// Validate a ticket by code
        /// </summary>
        [HttpGet("validate/{ticketCode}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> ValidateTicket(string ticketCode)
        {
            try
            {
                var isValid = await _ticketService.ValidateTicketAsync(ticketCode);

                return Ok(new { 
                    Success = true, 
                    IsValid = isValid,
                    TicketCode = ticketCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while validating ticket", Error = ex.Message });
            }
        }
    }

    // Request models
    public class BookTicketRequest
    {
        public Guid EventId { get; set; }
        public Guid EventPriceId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? DiscountCode { get; set; }
        public bool UseLoyaltyPoints { get; set; } = false;
    }

    public class ApplyPromotionRequest
    {
        public Guid TicketId { get; set; }
        public string DiscountCode { get; set; } = string.Empty;
    }

    public class UseLoyaltyPointsRequest
    {
        public Guid TicketId { get; set; }
        public int Points { get; set; }
    }
}