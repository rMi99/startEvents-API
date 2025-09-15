using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly ApplicationDbContext _context;

    public VenueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
    {
        return await _context.Venues
            .Include(v => v.Events)
            .OrderBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<Venue?> GetVenueByIdAsync(Guid id)
    {
        return await _context.Venues
            .Include(v => v.Events)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Venue> CreateVenueAsync(Venue venue)
    {
        venue.Id = Guid.NewGuid();
        venue.CreatedAt = DateTime.UtcNow;
        venue.ModifiedAt = DateTime.UtcNow;

        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();
        return venue;
    }

    public async Task<Venue> UpdateVenueAsync(Venue venue)
    {
        venue.ModifiedAt = DateTime.UtcNow;
        _context.Venues.Update(venue);
        await _context.SaveChangesAsync();
        return venue;
    }

    public async Task<bool> DeleteVenueAsync(Guid id)
    {
        var venue = await _context.Venues.FindAsync(id);
        if (venue == null)
            return false;

        _context.Venues.Remove(venue);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VenueExistsAsync(Guid id)
    {
        return await _context.Venues.AnyAsync(v => v.Id == id);
    }

    public async Task<bool> VenueHasEventsAsync(Guid id)
    {
        return await _context.Events.AnyAsync(e => e.VenueId == id);
    }

    public async Task<int> GetVenueEventCountAsync(Guid id)
    {
        return await _context.Events.CountAsync(e => e.VenueId == id);
    }

    public async Task<IEnumerable<Venue>> SearchVenuesAsync(string searchTerm)
    {
        return await _context.Venues
            .Include(v => v.Events)
            .Where(v => v.Name.Contains(searchTerm) || v.Location.Contains(searchTerm))
            .OrderBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Venue>> GetVenuesByLocationAsync(string location)
    {
        return await _context.Venues
            .Include(v => v.Events)
            .Where(v => v.Location.Contains(location))
            .OrderBy(v => v.Name)
            .ToListAsync();
    }
}