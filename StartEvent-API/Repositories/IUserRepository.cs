using StartEvent_API.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace StartEvent_API.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    }
}

