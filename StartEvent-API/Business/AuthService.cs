using Microsoft.AspNetCore.Identity;
using StartEvent_API.Data.Entities;
using StartEvent_API.Helper;
using StartEvent_API.Repositories;

namespace StartEvent_API.Business
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtService _jwtService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(
            IAuthRepository authRepository,
            IJwtService jwtService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
            _userManager = userManager;
            _signInManager = signInManager;
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
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
