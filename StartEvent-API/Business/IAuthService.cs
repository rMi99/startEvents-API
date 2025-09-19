using StartEvent_API.Data.Entities;
using StartEvent_API.Models.Auth;

namespace StartEvent_API.Business
{
    public interface IAuthService
    {
        Task<ApplicationUser?> RegisterAsync(ApplicationUser user, string password);
        Task<ApplicationUser?> LoginAsync(string email, string password);
        Task<bool> LogoutAsync(string userId);
        Task<ApplicationUser?> CreateAdminUserAsync(ApplicationUser user, string password);
        Task<List<ApplicationUser>> GetAllAdminUsersAsync();
        Task<EmailVerificationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request);
        Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request);
        Task<ResendVerificationResponse> ResendEmailVerificationAsync(ResendEmailVerificationRequest request);
    }
}

