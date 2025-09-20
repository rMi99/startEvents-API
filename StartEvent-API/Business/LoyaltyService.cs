using StartEvent_API.Data.Entities;
using StartEvent_API.Repositories;

namespace StartEvent_API.Business
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly ILoyaltyPointRepository _loyaltyPointRepository;
        private const decimal POINTS_EARNING_RATE = 0.10m; // 10% of purchase amount
        private const decimal POINTS_TO_CURRENCY_RATE = 1.0m; // 1 point = 1 LKR

        public LoyaltyService(ILoyaltyPointRepository loyaltyPointRepository)
        {
            _loyaltyPointRepository = loyaltyPointRepository;
        }

        public async Task<int> GetCustomerBalanceAsync(string customerId)
        {
            return await _loyaltyPointRepository.GetTotalPointsByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<LoyaltyPoint>> GetCustomerHistoryAsync(string customerId)
        {
            return await _loyaltyPointRepository.GetByCustomerIdAsync(customerId);
        }

        public async Task<bool> AddPointsAsync(string customerId, int points, string description)
        {
            if (points <= 0)
                return false;

            return await _loyaltyPointRepository.AddPointsAsync(customerId, points, description);
        }

        public async Task<bool> RedeemPointsAsync(string customerId, int points, string description = "Points redeemed")
        {
            if (points <= 0)
                return false;

            var canRedeem = await CanRedeemPointsAsync(customerId, points);
            if (!canRedeem)
                return false;

            return await _loyaltyPointRepository.RedeemPointsAsync(customerId, points);
        }

        public async Task<bool> CanRedeemPointsAsync(string customerId, int points)
        {
            if (points <= 0)
                return false;

            var balance = await GetCustomerBalanceAsync(customerId);
            return balance >= points;
        }

        public async Task<int> CalculateEarnedPointsAsync(decimal purchaseAmount)
        {
            if (purchaseAmount <= 0)
                return 0;

            // Calculate 10% of purchase amount as points
            var points = (int)Math.Floor(purchaseAmount * POINTS_EARNING_RATE);
            return points;
        }

        public async Task<decimal> CalculateDiscountFromPointsAsync(int points)
        {
            if (points <= 0)
                return 0;

            // 1 point = 1 LKR discount
            return points * POINTS_TO_CURRENCY_RATE;
        }
    }
}
