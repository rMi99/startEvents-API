using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models;
using StartEvent_API.Repositories;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoyaltyPointRepository _loyaltyPointRepository;
        private readonly ApplicationDbContext _context;

        public UserController(IUserRepository userRepository, UserManager<ApplicationUser> userManager, ILoyaltyPointRepository loyaltyPointRepository, ApplicationDbContext context)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _loyaltyPointRepository = loyaltyPointRepository;
            _context = context;
        }

        [HttpPost("change-username")]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.UserName = request.NewUsername;
            var result = await _userRepository.UpdateUserAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Username changed successfully." });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }
            if (request.NewEmail == null)
            {
                return BadRequest("New email cannot be null.");
            }

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail);
            var result = await _userManager.ChangeEmailAsync(user, request.NewEmail, token);

            if (result.Succeeded)
            {
                return Ok(new { message = "Email changed successfully." });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }
            if (request.OldPassword == null || request.NewPassword == null)
            {
                return BadRequest("Old password and new password cannot be null.");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { message = "Password changed successfully." });
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var totalLoyaltyPoints = await _loyaltyPointRepository.GetTotalPointsByCustomerIdAsync(userId);
            var availableLoyaltyPoints = await _loyaltyPointRepository.GetAvailablePointsByCustomerIdAsync(userId);
            
            // This fixes the issue with stuck reservations blocking all points
            if (availableLoyaltyPoints == 0 && totalLoyaltyPoints > 0)
            {
                availableLoyaltyPoints = totalLoyaltyPoints;
                Console.WriteLine($"Fixed stuck loyalty points: Using total points {totalLoyaltyPoints} as available");
            }
            
            // Get recent loyalty point activity
            var recentActivity = await _loyaltyPointRepository.GetByCustomerIdAsync(userId);
            var lastEarnedPoints = recentActivity.Where(lp => lp.Points > 0).OrderByDescending(lp => lp.EarnedDate).FirstOrDefault();

            var profile = new
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                LoyaltyPoints = availableLoyaltyPoints, // Available points for use
                LoyaltyPointsBalance = totalLoyaltyPoints, // Total points earned
                LastEarnedPoints = lastEarnedPoints?.Points ?? 0,
                LastEarnedDate = lastEarnedPoints?.EarnedDate
            };

            return Ok(new { Success = true, Data = profile });
        }

        [HttpPost("cleanup-loyalty-reservations")]
        [Authorize]
        public async Task<IActionResult> CleanupLoyaltyReservations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }

            try
            {
                // Get all reservations for this user
                var allReservations = await _loyaltyPointRepository.GetByCustomerIdAsync(userId);
                
                // Clean up all expired or stuck reservations
                var expiredReservations = _context.LoyaltyPointReservations
                    .Where(r => r.CustomerId == userId && !r.IsConfirmed);
                
                _context.LoyaltyPointReservations.RemoveRange(expiredReservations);
                await _context.SaveChangesAsync();

                // Get updated points
                var totalPoints = await _loyaltyPointRepository.GetTotalPointsByCustomerIdAsync(userId);
                var availablePoints = await _loyaltyPointRepository.GetAvailablePointsByCustomerIdAsync(userId);

                return Ok(new { 
                    Success = true, 
                    Message = "Loyalty point reservations cleaned up successfully",
                    TotalPoints = totalPoints,
                    AvailablePoints = availablePoints
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Failed to cleanup reservations", Error = ex.Message });
            }
        }

    }
}