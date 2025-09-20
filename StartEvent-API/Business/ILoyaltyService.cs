using StartEvent_API.Data.Entities;

namespace StartEvent_API.Business
{
    public interface ILoyaltyService
    {
        Task<int> GetCustomerBalanceAsync(string customerId);
        Task<IEnumerable<LoyaltyPoint>> GetCustomerHistoryAsync(string customerId);
        Task<bool> AddPointsAsync(string customerId, int points, string description);
        Task<bool> RedeemPointsAsync(string customerId, int points, string description = "Points redeemed");
        Task<bool> CanRedeemPointsAsync(string customerId, int points);
        Task<int> CalculateEarnedPointsAsync(decimal purchaseAmount);
        Task<decimal> CalculateDiscountFromPointsAsync(int points);
    }
}
