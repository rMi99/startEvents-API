using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories;

public interface IVenueRepository
{
    Task<IEnumerable<Venue>> GetAllVenuesAsync();
    Task<Venue?> GetVenueByIdAsync(Guid id);
    Task<Venue> CreateVenueAsync(Venue venue);
    Task<Venue> UpdateVenueAsync(Venue venue);
    Task<bool> DeleteVenueAsync(Guid id);
    Task<bool> VenueExistsAsync(Guid id);
    Task<bool> VenueHasEventsAsync(Guid id);
    Task<int> GetVenueEventCountAsync(Guid id);
    Task<IEnumerable<Venue>> SearchVenuesAsync(string searchTerm);
    Task<IEnumerable<Venue>> GetVenuesByLocationAsync(string location);
}