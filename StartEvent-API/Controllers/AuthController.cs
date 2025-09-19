using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using StartEvent_API.Data.Entities;
using StartEvent_API.Helper;
using StartEvent_API.Models.Auth;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
                    // Check if user exists but email is not verified
                    var userCheck = await _userManager.FindByEmailAsync(request.Email);
                    if (userCheck != null && !userCheck.IsEmailVerified)
                    {
                        return Unauthorized(new
                        {
                            message = "Email verification required",
                            error = "Please verify your email address before logging in. Check your inbox for the verification code.",
                            statusCode = 401,
                            requiresEmailVerification = true,
                            email = userCheck.Email
                        });
                    }

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

        #region Email Verification Endpoints

        [HttpPost("send-verification")]
        [ProducesResponseType(200, Type = typeof(EmailVerificationResponse))]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SendEmailVerification([FromBody] SendEmailVerificationRequest request)
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

                var result = await _authService.SendEmailVerificationAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        message = result.Message,
                        statusCode = 400
                    });
                }

                return Ok(new
                {
                    message = result.Message,
                    data = new
                    {
                        email = request.Email,
                        expiresAt = result.ExpiresAt
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while sending verification code",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(200, Type = typeof(VerifyEmailResponse))]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
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

                var result = await _authService.VerifyEmailAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        message = result.Message,
                        statusCode = 400
                    });
                }

                return Ok(new
                {
                    message = result.Message,
                    data = new
                    {
                        email = request.Email,
                        isVerified = true,
                        verifiedAt = DateTime.UtcNow
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while verifying email",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        [HttpPost("resend-verification")]
        [ProducesResponseType(200, Type = typeof(ResendVerificationResponse))]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResendEmailVerification([FromBody] ResendEmailVerificationRequest request)
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

                var result = await _authService.ResendEmailVerificationAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        message = result.Message,
                        statusCode = 400
                    });
                }

                return Ok(new
                {
                    message = result.Message,
                    data = new
                    {
                        email = request.Email,
                        expiresAt = result.ExpiresAt,
                        remainingAttempts = result.RemainingAttempts,
                        retryAfter = result.RetryAfter
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while resending verification code",
                    details = ex.Message,
                    statusCode = 500
                });
            }
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

        /// <summary>
        /// Gets the profile information of the currently logged-in user
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token or user not found",
                        statusCode = 401
                    });
                }

                var userProfile = await _authService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    return NotFound(new
                    {
                        message = "User profile not found",
                        statusCode = 404
                    });
                }

                return Ok(new
                {
                    message = "User profile retrieved successfully",
                    data = userProfile,
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while retrieving user profile",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        /// <summary>
        /// Updates the full name of the currently logged-in user
        /// </summary>
        [HttpPut("update-name")]
        [Authorize]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateUserName([FromBody] UpdateUserNameRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = errors,
                        statusCode = 400
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token or user not found",
                        statusCode = 401
                    });
                }

                var result = await _authService.UpdateUserNameAsync(userId, request.FullName);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "User not found or update failed",
                        statusCode = 404
                    });
                }

                return Ok(new
                {
                    message = "User name updated successfully",
                    data = new
                    {
                        fullName = request.FullName,
                        updatedAt = DateTime.UtcNow
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while updating user name",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        /// <summary>
        /// Initiates a password reset by sending a reset token to the user's email
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = errors,
                        statusCode = 400
                    });
                }

                var result = await _authService.InitiatePasswordResetAsync(request.Email);

                return Ok(new
                {
                    message = result.Message,
                    data = new
                    {
                        email = request.Email,
                        success = result.Success,
                        resetToken = result.ResetToken // Remove this in production - should only be sent via email
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while processing password reset request",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        /// <summary>
        /// Confirms password reset with the reset token and sets a new password
        /// </summary>
        [HttpPost("confirm-password-reset")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = errors,
                        statusCode = 400
                    });
                }

                var result = await _authService.ConfirmPasswordResetAsync(request.Email, request.ResetToken, request.NewPassword);

                if (!result)
                {
                    return BadRequest(new
                    {
                        message = "Invalid reset token or password reset failed",
                        error = "The reset token may be invalid, expired, or the email address may not exist",
                        statusCode = 400
                    });
                }

                return Ok(new
                {
                    message = "Password reset successfully",
                    data = new
                    {
                        email = request.Email,
                        resetAt = DateTime.UtcNow
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while confirming password reset",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)),
                        statusCode = 400
                    });
                }

                var result = await _authService.SendPasswordResetOtpAsync(request.Email);

                if (!result.Success)
                {
                    if (result.RemainingAttempts == 0)
                    {
                        return StatusCode(429, new
                        {
                            message = result.Message,
                            statusCode = 429,
                            data = new
                            {
                                remainingAttempts = result.RemainingAttempts,
                                retryAfter = "1 hour"
                            }
                        });
                    }

                    return BadRequest(new
                    {
                        message = result.Message,
                        statusCode = 400
                    });
                }

                return Ok(new
                {
                    message = result.Message,
                    data = new
                    {
                        email = request.Email,
                        expiresAt = result.ExpiresAt,
                        remainingAttempts = result.RemainingAttempts,
                        sentAt = DateTime.UtcNow
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while processing forgot password request",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        [HttpPost("verify-reset-otp")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> VerifyPasswordResetOtp([FromBody] VerifyPasswordResetOtpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)),
                        statusCode = 400
                    });
                }

                var result = await _authService.VerifyPasswordResetOtpAsync(request.Email, request.Otp);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        message = result.Message,
                        statusCode = 400
                    });
                }

                return Ok(new
                {
                    message = result.Message,
                    data = new
                    {
                        email = request.Email,
                        resetToken = result.ResetToken,
                        verifiedAt = DateTime.UtcNow,
                        tokenExpiresAt = DateTime.UtcNow.AddMinutes(30)
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while verifying OTP",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        [HttpPost("reset-password-otp")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)),
                        statusCode = 400
                    });
                }

                var success = await _authService.ResetPasswordWithOtpAsync(request.Email, request.ResetToken, request.NewPassword);

                if (!success)
                {
                    return BadRequest(new
                    {
                        message = "Failed to reset password. The reset token may be invalid, expired, or the email may not exist.",
                        statusCode = 400
                    });
                }

                return Ok(new
                {
                    message = "Password has been reset successfully. You can now log in with your new password.",
                    data = new
                    {
                        email = request.Email,
                        resetAt = DateTime.UtcNow
                    },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = "An unexpected error occurred while resetting password",
                    details = ex.Message,
                    statusCode = 500
                });
            }
        }

        #endregion
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
