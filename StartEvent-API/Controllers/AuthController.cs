using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using StartEvent_API.Data.Entities;
using StartEvent_API.Helper;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthService authService, IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Address = request.Address,
                DateOfBirth = request.DateOfBirth,
                OrganizationName = request.OrganizationName,
                OrganizationContact = request.OrganizationContact
            };

            var result = await _authService.RegisterAsync(user, request.Password);
            
            if (result == null)
            {
                return BadRequest(new { message = "User registration failed" });
            }

            // Generate JWT token
            var roles = new List<string> { "User" }; // Default role
            var token = _jwtService.GenerateToken(result, roles);

            return Ok(new { 
                message = "User registered successfully", 
                user = result,
                token = token
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            var result = await _authService.LoginAsync(request.Email, request.Password);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate JWT token
            var roles = new List<string> { "User" }; // Default role - in real app, get from user roles
            var token = _jwtService.GenerateToken(result, roles);

            return Ok(new { 
                message = "Login successful", 
                user = result,
                token = token
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logout successful", success = result });
        }
    }

    // Request models
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrganizationContact { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
