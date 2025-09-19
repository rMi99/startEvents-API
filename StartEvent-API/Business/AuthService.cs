using Microsoft.AspNetCore.Identity;
using StartEvent_API.Data.Entities;
using StartEvent_API.Helper;
using StartEvent_API.Models.Auth;
using StartEvent_API.Models.Email;
using StartEvent_API.Repositories;
using StartEvent_API.Services.Email;

namespace StartEvent_API.Business
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtService _jwtService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepository,
            IJwtService jwtService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ApplicationUser?> RegisterAsync(ApplicationUser user, string password)
        {
            try
            {
                // Check if user already exists
                if (await _authRepository.UserExistsAsync(user.Email ?? string.Empty))
                {
                    return null;
                }

                // Set additional properties
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.IsEmailVerified = false; // Email verification required

                var result = await _authRepository.CreateUserAsync(user, password);

                if (!result.Succeeded)
                {
                    // Log the specific errors
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"User creation failed: {errors}");
                    return null;
                }

                // Assign role based on organization name
                string role = string.IsNullOrEmpty(user.OrganizationName) ? "Customer" : "Organizer";
                var roleResult = await _authRepository.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    // Log role assignment errors
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"Role assignment failed: {roleErrors}");
                    return null;
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                // Send welcome email
                await SendWelcomeEmailAsync(user);

                // Generate and send email verification
                await GenerateAndSendEmailVerificationAsync(user);

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration exception: {ex.Message}");
                return null;
            }
        }

        public async Task<ApplicationUser?> LoginAsync(string email, string password)
        {
            try
            {
                // Get user by email
                var user = await _authRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return null;
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return null;
                }

                // Check if email is verified
                if (!user.IsEmailVerified)
                {
                    return null; // Email not verified, login blocked
                }

                // Verify password
                var passwordValid = await _authRepository.CheckPasswordAsync(user, password);
                if (!passwordValid)
                {
                    return null;
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                return user;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            try
            {
                // In a stateless JWT implementation, logout is typically handled client-side
                // by removing the token. However, you could implement token blacklisting here
                // if needed for additional security.
                await Task.CompletedTask;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ApplicationUser?> CreateAdminUserAsync(ApplicationUser user, string password)
        {
            try
            {
                // Check if user already exists
                if (await _authRepository.UserExistsAsync(user.Email ?? string.Empty))
                {
                    return null;
                }

                // Set additional properties for admin user
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.EmailConfirmed = true; // Admin users are automatically verified

                var result = await _authRepository.CreateUserAsync(user, password);
                if (!result.Succeeded)
                {
                    return null;
                }

                // Get the created user
                var createdUser = await _authRepository.GetUserByEmailAsync(user.Email ?? string.Empty);
                if (createdUser == null)
                {
                    return null;
                }

                // Assign Admin role
                await _authRepository.AddToRoleAsync(createdUser, "Admin");

                // Send welcome email
                await SendWelcomeEmailAsync(createdUser);

                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin user with email {Email}", user.Email);
                return null;
            }
        }

        public async Task<List<ApplicationUser>> GetAllAdminUsersAsync()
        {
            try
            {
                var adminUsers = await _authRepository.GetUsersByRoleAsync("Admin");
                return adminUsers.OrderBy(u => u.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin users");
                return new List<ApplicationUser>();
            }
        }

        private async Task SendWelcomeEmailAsync(ApplicationUser user)
        {
            try
            {
                var welcomeEmail = new WelcomeEmailTemplate
                {
                    To = new EmailRecipient
                    {
                        Email = user.Email ?? string.Empty,
                        Name = user.FullName ?? user.UserName ?? "User"
                    },
                    User = user,
                    Subject = $"Welcome to StartEvent, {user.FullName ?? user.UserName}!",
                    // You can add verification and dashboard links here if needed
                    // VerificationLink = "https://yourdomain.com/verify-email?token=...",
                    // DashboardLink = "https://yourdomain.com/dashboard"
                };

                var result = await _emailService.SendWelcomeEmailAsync(welcomeEmail);

                if (result.Success)
                {
                    _logger.LogInformation("Welcome email sent successfully to {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send welcome email to {Email}: {Error}",
                        user.Email, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending welcome email to {Email}", user.Email);
            }
        }

        #region Email Verification

        public async Task<EmailVerificationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request)
        {
            try
            {
                var user = await _authRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                if (user.IsEmailVerified)
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "Email already verified"
                    };
                }

                await GenerateAndSendEmailVerificationAsync(user);

                return new EmailVerificationResponse
                {
                    Success = true,
                    Message = "Verification code sent successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification to {Email}", request.Email);
                return new EmailVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred while sending verification code"
                };
            }
        }

        public async Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request)
        {
            try
            {
                var user = await _authRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return new VerifyEmailResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                if (user.IsEmailVerified)
                {
                    return new VerifyEmailResponse
                    {
                        Success = true,
                        Message = "Email already verified"
                    };
                }

                // Check if verification code exists and hasn't expired
                if (string.IsNullOrEmpty(user.EmailVerificationCode))
                {
                    return new VerifyEmailResponse
                    {
                        Success = false,
                        Message = "No verification code found. Please request a new one"
                    };
                }

                if (user.EmailVerificationCodeExpiry == null || user.EmailVerificationCodeExpiry < DateTime.UtcNow)
                {
                    return new VerifyEmailResponse
                    {
                        Success = false,
                        Message = "Verification code has expired. Please request a new one"
                    };
                }

                // Check if the provided code matches
                if (user.EmailVerificationCode != request.VerificationCode)
                {
                    return new VerifyEmailResponse
                    {
                        Success = false,
                        Message = "Invalid verification code"
                    };
                }

                // Mark email as verified and clear verification data
                user.IsEmailVerified = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationCodeExpiry = null;

                await _authRepository.UpdateUserAsync(user);

                _logger.LogInformation("Email verified successfully for user {Email}", user.Email);

                return new VerifyEmailResponse
                {
                    Success = true,
                    Message = "Email verified successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", request.Email);
                return new VerifyEmailResponse
                {
                    Success = false,
                    Message = "An error occurred while verifying email"
                };
            }
        }

        public async Task<ResendVerificationResponse> ResendEmailVerificationAsync(ResendEmailVerificationRequest request)
        {
            try
            {
                var user = await _authRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return new ResendVerificationResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                if (user.IsEmailVerified)
                {
                    return new ResendVerificationResponse
                    {
                        Success = false,
                        Message = "Email already verified"
                    };
                }

                await GenerateAndSendEmailVerificationAsync(user);

                return new ResendVerificationResponse
                {
                    Success = true,
                    Message = "New verification code sent successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email verification to {Email}", request.Email);
                return new ResendVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred while resending verification code"
                };
            }
        }

        private async Task GenerateAndSendEmailVerificationAsync(ApplicationUser user)
        {
            // Generate 6-digit verification code
            var random = new Random();
            var verificationCode = random.Next(100000, 999999).ToString();

            // Set expiry time to 15 minutes from now
            var expiryTime = DateTime.UtcNow.AddMinutes(15);

            // Update user with verification code and expiry
            user.EmailVerificationCode = verificationCode;
            user.EmailVerificationCodeExpiry = expiryTime;

            await _authRepository.UpdateUserAsync(user);

            // Send verification email
            var verificationEmail = new EmailVerificationEmailTemplate
            {
                To = new EmailRecipient
                {
                    Email = user.Email ?? string.Empty,
                    Name = user.FullName ?? user.UserName ?? "User"
                },
                User = user,
                Subject = "Verify Your Email Address",
                VerificationCode = verificationCode,
                VerificationLink = string.Empty // We're focusing on code verification, but link can be added if needed
            };

            var result = await _emailService.SendEmailVerificationEmailAsync(verificationEmail);

            if (result.Success)
            {
                _logger.LogInformation("Email verification code sent successfully to {Email}", user.Email);
            }
            else
            {
                _logger.LogWarning("Failed to send email verification code to {Email}: {Error}",
                    user.Email, result.ErrorMessage);
            }
        }

        #endregion

        #region User Profile Management

        public async Task<UserProfileResponse?> GetUserProfileAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);

                return new UserProfileResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Address = user.Address,
                    OrganizationName = user.OrganizationName,
                    OrganizationContact = user.OrganizationContact,
                    DateOfBirth = user.DateOfBirth,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    Roles = roles.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving user profile for user ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateUserNameAsync(string userId, string newFullName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return false;
                }

                user.FullName = newFullName;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User name updated successfully for user ID: {UserId}", userId);
                    return true;
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to update user name for user ID: {UserId}. Errors: {Errors}", userId, errors);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user name for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<ResetPasswordResponse> InitiatePasswordResetAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // For security reasons, don't reveal whether the email exists
                    _logger.LogWarning("Password reset attempted for non-existent email: {Email}", email);
                    return new ResetPasswordResponse
                    {
                        Success = true,
                        Message = "If the email address exists in our system, a password reset link has been sent."
                    };
                }

                // Generate password reset token
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                // TODO: Send password reset email with token
                // For now, we'll return the token (in production, this should be sent via email only)
                _logger.LogInformation("Password reset token generated for user: {Email}", email);

                return new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Password reset instructions have been sent to your email address.",
                    ResetToken = resetToken // In production, remove this and send via email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initiating password reset for email: {Email}", email);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your password reset request. Please try again later."
                };
            }
        }

        public async Task<bool> ConfirmPasswordResetAsync(string email, string resetToken, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Password reset confirmation attempted for non-existent email: {Email}", email);
                    return false;
                }

                // Reset the password using the token
                var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successfully for user: {Email}", email);

                    // Update last login time
                    user.LastLogin = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    return true;
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to reset password for user: {Email}. Errors: {Errors}", email, errors);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while confirming password reset for email: {Email}", email);
                return false;
            }
        }

        #endregion
    }
}
