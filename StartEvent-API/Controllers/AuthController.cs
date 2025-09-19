using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using StartEvent_API.Data.Entities;
using StartEvent_API.Helper;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(
                    IAuthService authService,
                    IJwtService jwtService,
                    UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _jwtService = jwtService;
            _userManager = userManager;
        }
        [HttpPost("register")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestWrapper requestWrapper)
        {
            try
            {
                // Check if request wrapper is null
                if (requestWrapper?.Request == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid request format",
                        error = "Request body must contain a 'request' object with user registration data",
                        statusCode = 400
                    });
                }

                if (!ModelState.IsValid)
                {
                    return UnprocessableEntity(new
                    {
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)),
                        statusCode = 422
                    });
                }

                var request = requestWrapper.Request;

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return Conflict(new
                    {
                        message = "User already exists",
                        error = $"A user with email '{request.Email}' already exists",
                        statusCode = 409
                    });
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
                    return BadRequest(new
                    {
                        message = "User registration failed",
                        error = "Failed to create user. Please check your input data and try again.",
                        statusCode = 400
                    });
                }

                // Get user roles for JWT token
                var roles = await _userManager.GetRolesAsync(result);
                var token = _jwtService.GenerateToken(result, roles);

                // Determine assigned role for response
                string assignedRole = string.IsNullOrEmpty(request.OrganizationName) ? "Customer" : "Organizer";

                return Ok(new
                {
                    message = "User registered successfully",
                    data = new
                    {
                        user = new
                        {
                            id = result.Id,
                            email = result.Email,
                            fullName = result.FullName,
                            address = result.Address,
                            dateOfBirth = result.DateOfBirth,
                            organizationName = result.OrganizationName,
                            organizationContact = result.OrganizationContact,
                            assignedRole = assignedRole,
                            createdAt = result.CreatedAt
                        },
                        token = token,
                        roles = roles
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred during registration",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }
        [HttpPost("login")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return UnprocessableEntity(new
                    {
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)),
                        statusCode = 422
                    });
                }

                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new
                    {
                        message = "Invalid request",
                        error = "Email and password are required",
                        statusCode = 400
                    });
                }

                var result = await _authService.LoginAsync(request.Email, request.Password);

                if (result == null)
                {
                    return Unauthorized(new
                    {
                        message = "Authentication failed",
                        error = "Invalid email or password, or account may be inactive",
                        statusCode = 401
                    });
                }

                // Generate JWT token
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(new
                    {
                        message = "Authentication failed",
                        error = "User not found",
                        statusCode = 401
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtService.GenerateToken(user, roles);

                return Ok(new
                {
                    message = "Login successful",
                    data = new
                    {
                        user = new
                        {
                            id = result.Id,
                            email = result.Email,
                            fullName = result.FullName,
                            address = result.Address,
                            organizationName = result.OrganizationName,
                            lastLogin = result.LastLogin
                        },
                        roles = roles,
                        token = token
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred during login",
                    details = ex.Message,
                    statusCode = 500
                });
            }
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

        /// <summary>
        /// Creates a new admin user. Only accessible by existing admin users.
        /// </summary>
        [HttpPost("create-admin")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                        statusCode = 400
                    });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return Conflict(new
                    {
                        message = "User already exists",
                        error = $"A user with email '{request.Email}' already exists",
                        statusCode = 409
                    });
                }

                var newAdminUser = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    Address = request.Address,
                    DateOfBirth = request.DateOfBirth,
                    OrganizationName = request.OrganizationName,
                    OrganizationContact = request.OrganizationContact,
                    EmailConfirmed = true
                };

                var createdUser = await _authService.CreateAdminUserAsync(newAdminUser, request.Password);

                if (createdUser == null)
                {
                    return StatusCode(500, new
                    {
                        message = "Admin user creation failed",
                        error = "Unable to create admin user. Please try again.",
                        statusCode = 500
                    });
                }

                // Get the roles for the created user
                var roles = await _userManager.GetRolesAsync(createdUser);
                var token = _jwtService.GenerateToken(createdUser, roles);

                return Ok(new
                {
                    message = "Admin user created successfully",
                    data = new
                    {
                        user = new
                        {
                            id = createdUser.Id,
                            email = createdUser.Email,
                            fullName = createdUser.FullName,
                            address = createdUser.Address,
                            dateOfBirth = createdUser.DateOfBirth,
                            organizationName = createdUser.OrganizationName,
                            organizationContact = createdUser.OrganizationContact,
                            assignedRole = "Admin",
                            createdAt = createdUser.CreatedAt
                        },
                        roles
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred during admin user creation",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        /// <summary>
        /// Gets all admin users in the system. Only accessible by existing admin users.
        /// </summary>
        [HttpGet("admin-users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllAdminUsers()
        {
            try
            {
                var adminUsers = await _authService.GetAllAdminUsersAsync();

                var adminUserDtos = adminUsers.Select(user => new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    address = user.Address,
                    dateOfBirth = user.DateOfBirth,
                    organizationName = user.OrganizationName,
                    organizationContact = user.OrganizationContact,
                    isActive = user.IsActive,
                    emailConfirmed = user.EmailConfirmed,
                    createdAt = user.CreatedAt,
                    lastLogin = user.LastLogin
                }).ToList();

                return Ok(new
                {
                    message = "Admin users retrieved successfully",
                    data = new
                    {
                        totalCount = adminUserDtos.Count,
                        adminUsers = adminUserDtos
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while retrieving admin users",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }
    }

    // Request models
    public class RegisterRequestWrapper
    {
        [Required]
        public RegisterRequest Request { get; set; } = new RegisterRequest();
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }

        public string? OrganizationName { get; set; }
        public string? OrganizationContact { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateAdminRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrganizationContact { get; set; }
    }
}
