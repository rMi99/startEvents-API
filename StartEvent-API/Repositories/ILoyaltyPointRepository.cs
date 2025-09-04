using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public interface ILoyaltyPointRepository
    {
        Task<LoyaltyPoint?> GetByIdAsync(Guid id);
        Task<IEnumerable<LoyaltyPoint>> GetByCustomerIdAsync(string customerId);
        Task<int> GetTotalPointsByCustomerIdAsync(string customerId);
        Task<LoyaltyPoint> CreateAsync(LoyaltyPoint loyaltyPoint);
        Task<LoyaltyPoint> UpdateAsync(LoyaltyPoint loyaltyPoint);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> RedeemPointsAsync(string customerId, int points);
        Task<bool> AddPointsAsync(string customerId, int points, string description);
        Task<int> GetAvailablePointsByCustomerIdAsync(string customerId);
    }
}
