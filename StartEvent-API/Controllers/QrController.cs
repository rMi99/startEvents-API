using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using System.Security.Claims;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QrController : ControllerBase
    {
        private readonly IQrService _qrService;

        public QrController(IQrService qrService)
        {
            _qrService = qrService;
        }

        /// <summary>
        /// Generate QR code for a ticket
        /// </summary>
        /// <param name="request">Ticket ID for QR generation</param>
        /// <returns>QR code generation result with image path and base64 data</returns>
        [HttpPost("generate")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GenerateQrCode([FromBody] GenerateQrRequest request)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(customerId))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _qrService.GenerateQrCodeAsync(request.TicketId, customerId);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = result.Success,
                        message = result.Message,
                        errors = result.Errors
                    });
                }

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    data = new
                    {
                        ticketId = result.TicketId,
                        ticketCode = result.TicketCode,
                        qrCodePath = result.QrCodePath,
                        qrCodeBase64 = result.QrCodeBase64
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while generating QR code",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get QR code image by ticket code
        /// </summary>
        /// <param name="ticketCode">The ticket code to retrieve QR image for</param>
        /// <returns>QR code image as PNG</returns>
        [HttpGet("{ticketCode}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQrCodeImage(string ticketCode)
        {
            try
            {
                var qrCodeImage = await _qrService.GetQrCodeImageAsync(ticketCode);
                
                if (qrCodeImage == null)
                {
                    return NotFound(new { success = false, message = "QR code not found" });
                }

                return File(qrCodeImage, "image/png", $"{ticketCode}.png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving QR code",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Validate QR code ticket
        /// </summary>
        /// <param name="ticketCode">The ticket code to validate</param>
        /// <returns>Ticket validation result</returns>
        [HttpGet("validate/{ticketCode}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> ValidateQrCode(string ticketCode)
        {
            try
            {
                var result = await _qrService.ValidateQrCodeAsync(ticketCode);

                return Ok(new
                {
                    success = true,
                    isValid = result.IsValid,
                    message = result.Message,
                    data = new
                    {
                        ticketCode = result.TicketCode,
                        ticket = result.Ticket != null ? new
                        {
                            result.Ticket.Id,
                            result.Ticket.TicketNumber,
                            result.Ticket.TicketCode,
                            result.Ticket.IsPaid,
                            result.Ticket.PurchaseDate,
                            customer = new
                            {
                                result.Ticket.Customer.Id,
                                result.Ticket.Customer.Email,
                                result.Ticket.Customer.FullName
                            },
                            eventInfo = new
                            {
                                result.Ticket.Event.Id,
                                result.Ticket.Event.Title,
                                result.Ticket.Event.EventDate
                            }
                        } : null
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while validating QR code",
                    errors = new[] { ex.Message }
                });
            }
        }
    }

    // Request models
    public class GenerateQrRequest
    {
        public Guid TicketId { get; set; }
    }
}
