using StartEvent_API.Data.Entities;

namespace StartEvent_API.Business
{
    public interface IAuthService
    {
        Task<ApplicationUser?> RegisterAsync(ApplicationUser user, string password);
        Task<ApplicationUser?> LoginAsync(string email, string password);
        Task<bool> LogoutAsync(string userId);
    }
}

