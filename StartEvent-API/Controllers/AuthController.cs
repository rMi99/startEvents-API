using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StartEvent_API.Data.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;

        public AuthController(UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "User created successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded) return Unauthorized();

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
