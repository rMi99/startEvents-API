using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models.DTOs;
using StartEvent_API.Repositories;

namespace StartEvent_API.Controllers;

[ApiController]
[Route("api/public/venues")]
public class PublicVenuesController : ControllerBase
{
    private readonly IVenueRepository _venueRepository;

    public PublicVenuesController(IVenueRepository venueRepository)
    {
        _venueRepository = venueRepository;
    }

    /// <summary>
    /// Gets all venues without authentication - public endpoint
    /// </summary>
    /// <param name="search">Optional search term to filter venues by name</param>
    /// <param name="location">Optional location filter</param>
    /// <returns>List of all venues</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetAllVenues(
        [FromQuery] string? search = null,
        [FromQuery] string? location = null)
    {
        IEnumerable<Venue> venues;

        if (!string.IsNullOrEmpty(search))
        {
            venues = await _venueRepository.SearchVenuesAsync(search);
        }
        else if (!string.IsNullOrEmpty(location))
        {
            venues = await _venueRepository.GetVenuesByLocationAsync(location);
        }
        else
        {
            venues = await _venueRepository.GetAllVenuesAsync();
        }

        var venueDtos = venues.Select(v => new VenueDto
        {
            Id = v.Id,
            Name = v.Name,
            Location = v.Location,
            Capacity = v.Capacity,
            CreatedAt = v.CreatedAt,
            ModifiedAt = v.ModifiedAt,
            EventCount = v.Events.Count()
        }).ToList();

        return Ok(venueDtos);
    }

    /// <summary>
    /// Gets a specific venue by ID without authentication - public endpoint
    /// </summary>
    /// <param name="id">The venue ID</param>
    /// <returns>Venue details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<VenueDto>> GetVenue(Guid id)
    {
        var venue = await _venueRepository.GetVenueByIdAsync(id);
        if (venue == null)
        {
            return NotFound("Venue not found");
        }

        var venueDto = new VenueDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Location = venue.Location,
            Capacity = venue.Capacity,
            CreatedAt = venue.CreatedAt,
            ModifiedAt = venue.ModifiedAt,
            EventCount = venue.Events.Count()
        };

        return Ok(venueDto);
    }
}