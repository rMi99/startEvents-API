using StartEvent_API.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace StartEvent_API.Repositories
{
    public interface IAuthRepository
    {
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
        Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role);
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<bool> UserExistsAsync(string email);
    }
}

