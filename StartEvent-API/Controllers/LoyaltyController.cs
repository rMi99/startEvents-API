using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;

        public LoyaltyController(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService;
        }

        /// <summary>
        /// Get loyalty point balance for a customer
        /// </summary>
        [HttpGet("balance")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetBalance()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated" });

                var balance = await _loyaltyService.GetCustomerBalanceAsync(userId);
                return Ok(new { 
                    UserId = userId,
                    Balance = balance,
                    DiscountValue = await _loyaltyService.CalculateDiscountFromPointsAsync(balance)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to retrieve loyalty balance", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get loyalty point balance for a specific customer (Admin/Organizer only)
        /// </summary>
        [HttpGet("balance/{userId}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> GetBalance(string userId)
        {
            try
            {
                var balance = await _loyaltyService.GetCustomerBalanceAsync(userId);
                return Ok(new { 
                    UserId = userId,
                    Balance = balance,
                    DiscountValue = await _loyaltyService.CalculateDiscountFromPointsAsync(balance)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to retrieve loyalty balance", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get loyalty point history for current customer
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated" });

                var history = await _loyaltyService.GetCustomerHistoryAsync(userId);
                return Ok(new { 
                    UserId = userId,
                    History = history.Select(h => new {
                        Id = h.Id,
                        Points = h.Points,
                        Description = h.Description,
                        EarnedDate = h.EarnedDate,
                        Type = h.Points > 0 ? "Earned" : "Redeemed"
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to retrieve loyalty history", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get loyalty point history for a specific customer (Admin/Organizer only)
        /// </summary>
        [HttpGet("history/{userId}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> GetHistory(string userId)
        {
            try
            {
                var history = await _loyaltyService.GetCustomerHistoryAsync(userId);
                return Ok(new { 
                    UserId = userId,
                    History = history.Select(h => new {
                        Id = h.Id,
                        Points = h.Points,
                        Description = h.Description,
                        EarnedDate = h.EarnedDate,
                        Type = h.Points > 0 ? "Earned" : "Redeemed"
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to retrieve loyalty history", Error = ex.Message });
            }
        }

        /// <summary>
        /// Redeem loyalty points
        /// </summary>
        [HttpPost("redeem")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsRequest request)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated" });

                if (request.Points <= 0)
                    return BadRequest(new { Message = "Points must be greater than zero" });

                // Check if user has enough points
                var canRedeem = await _loyaltyService.CanRedeemPointsAsync(userId, request.Points);
                if (!canRedeem)
                {
                    var currentBalance = await _loyaltyService.GetCustomerBalanceAsync(userId);
                    return BadRequest(new { 
                        Message = "Insufficient loyalty points", 
                        RequestedPoints = request.Points,
                        CurrentBalance = currentBalance 
                    });
                }

                var success = await _loyaltyService.RedeemPointsAsync(userId, request.Points, request.Description ?? "Points redeemed for purchase");
                if (success)
                {
                    var newBalance = await _loyaltyService.GetCustomerBalanceAsync(userId);
                    var discountValue = await _loyaltyService.CalculateDiscountFromPointsAsync(request.Points);
                    
                    return Ok(new { 
                        Success = true,
                        Message = "Points redeemed successfully",
                        RedeemedPoints = request.Points,
                        DiscountValue = discountValue,
                        RemainingBalance = newBalance
                    });
                }

                return StatusCode(500, new { Message = "Failed to redeem points" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to redeem loyalty points", Error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate potential discount from points
        /// </summary>
        [HttpGet("calculate-discount/{points}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CalculateDiscount(int points)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated" });

                if (points <= 0)
                    return BadRequest(new { Message = "Points must be greater than zero" });

                var currentBalance = await _loyaltyService.GetCustomerBalanceAsync(userId);
                var canRedeem = await _loyaltyService.CanRedeemPointsAsync(userId, points);
                var discountValue = await _loyaltyService.CalculateDiscountFromPointsAsync(points);

                return Ok(new { 
                    RequestedPoints = points,
                    CurrentBalance = currentBalance,
                    CanRedeem = canRedeem,
                    DiscountValue = discountValue
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to calculate discount", Error = ex.Message });
            }
        }

        /// <summary>
        /// Add loyalty points (Admin only)
        /// </summary>
        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddPoints([FromBody] AddPointsRequest request)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (request.Points <= 0)
                    return BadRequest(new { Message = "Points must be greater than zero" });

                if (string.IsNullOrEmpty(request.UserId))
                    return BadRequest(new { Message = "User ID is required" });

                var success = await _loyaltyService.AddPointsAsync(request.UserId, request.Points, request.Description ?? "Points added by admin");
                if (success)
                {
                    var newBalance = await _loyaltyService.GetCustomerBalanceAsync(request.UserId);
                    return Ok(new { 
                        Success = true,
                        Message = "Points added successfully",
                        AddedPoints = request.Points,
                        NewBalance = newBalance
                    });
                }

                return StatusCode(500, new { Message = "Failed to add points" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to add loyalty points", Error = ex.Message });
            }
        }
    }

    // Request models
    public class RedeemPointsRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than zero")]
        public int Points { get; set; }
        
        public string? Description { get; set; }
    }

    public class AddPointsRequest
    {
        [Required]
        public string UserId { get; set; } = default!;
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than zero")]
        public int Points { get; set; }
        
        public string? Description { get; set; }
    }
}
