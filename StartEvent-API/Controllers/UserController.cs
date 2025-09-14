using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public UserController(IUserRepository userRepository, UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
        }

        [HttpPost("change-username")]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
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
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { message = "Password changed successfully." });
            }

            return BadRequest(result.Errors);
        }
    }
}