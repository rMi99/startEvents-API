using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public interface IDiscountRepository
    {
        Task<Discount?> GetByIdAsync(Guid id);
        Task<Discount?> GetByCodeAsync(string code);
        Task<IEnumerable<Discount>> GetByEventIdAsync(Guid eventId);
        Task<IEnumerable<Discount>> GetActiveDiscountsAsync();
        Task<Discount> CreateAsync(Discount discount);
        Task<Discount> UpdateAsync(Discount discount);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IsCodeValidAsync(string code, Guid? eventId = null);
        Task<decimal> CalculateDiscountAsync(string code, decimal amount, Guid? eventId = null);
    }
}
