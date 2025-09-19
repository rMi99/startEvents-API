using Microsoft.AspNetCore.Identity;
using StartEvent_API.Data.Entities;
using StartEvent_API.Helper;
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
    }
}
