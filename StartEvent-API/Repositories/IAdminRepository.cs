using Microsoft.AspNetCore.Identity;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public interface IAdminRepository
    {
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password);
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
        Task<IdentityResult> DeleteUserAsync(ApplicationUser user);
        Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role);
        Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
        Task<bool> UserExistsAsync(string email);
        Task<bool> RoleExistsAsync(string role);
    }
}
