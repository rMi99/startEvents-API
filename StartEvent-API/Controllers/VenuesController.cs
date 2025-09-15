using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models.DTOs;
using StartEvent_API.Repositories;

namespace StartEvent_API.Controllers;

[ApiController]
[Route("api/venues")]
[Authorize(Roles = "Organizer,Admin")]
public class VenuesController : ControllerBase
{
    private readonly IVenueRepository _venueRepository;

    public VenuesController(IVenueRepository venueRepository)
    {
        _venueRepository = venueRepository;
    }

    // GET: api/venues
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetVenues(
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

    // GET: api/venues/{id}
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

    // POST: api/venues
    [HttpPost]
    public async Task<ActionResult<VenueDto>> CreateVenue([FromBody] CreateVenueDto createVenueDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var venue = new Venue
        {
            Name = createVenueDto.Name,
            Location = createVenueDto.Location,
            Capacity = createVenueDto.Capacity
        };

        var createdVenue = await _venueRepository.CreateVenueAsync(venue);

        var venueDto = new VenueDto
        {
            Id = createdVenue.Id,
            Name = createdVenue.Name,
            Location = createdVenue.Location,
            Capacity = createdVenue.Capacity,
            CreatedAt = createdVenue.CreatedAt,
            ModifiedAt = createdVenue.ModifiedAt,
            EventCount = 0
        };

        return CreatedAtAction(nameof(GetVenue), new { id = createdVenue.Id }, venueDto);
    }

    // PUT: api/venues/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<VenueDto>> UpdateVenue(Guid id, [FromBody] UpdateVenueDto updateVenueDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingVenue = await _venueRepository.GetVenueByIdAsync(id);
        if (existingVenue == null)
        {
            return NotFound("Venue not found");
        }

        existingVenue.Name = updateVenueDto.Name;
        existingVenue.Location = updateVenueDto.Location;
        existingVenue.Capacity = updateVenueDto.Capacity;

        var updatedVenue = await _venueRepository.UpdateVenueAsync(existingVenue);

        var venueDto = new VenueDto
        {
            Id = updatedVenue.Id,
            Name = updatedVenue.Name,
            Location = updatedVenue.Location,
            Capacity = updatedVenue.Capacity,
            CreatedAt = updatedVenue.CreatedAt,
            ModifiedAt = updatedVenue.ModifiedAt,
            EventCount = updatedVenue.Events.Count()
        };

        return Ok(venueDto);
    }

    // DELETE: api/venues/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteVenue(Guid id)
    {
        var venue = await _venueRepository.GetVenueByIdAsync(id);
        if (venue == null)
        {
            return NotFound("Venue not found");
        }

        // Check if venue has events
        var hasEvents = await _venueRepository.VenueHasEventsAsync(id);
        if (hasEvents)
        {
            return BadRequest("Cannot delete venue that has events. Please delete or move all events first.");
        }

        var deleted = await _venueRepository.DeleteVenueAsync(id);
        if (!deleted)
        {
            return BadRequest("Failed to delete venue");
        }

        return NoContent();
    }

    // GET: api/venues/{id}/events-count
    [HttpGet("{id}/events-count")]
    public async Task<ActionResult<int>> GetVenueEventCount(Guid id)
    {
        var venueExists = await _venueRepository.VenueExistsAsync(id);
        if (!venueExists)
        {
            return NotFound("Venue not found");
        }

        var eventCount = await _venueRepository.GetVenueEventCountAsync(id);
        return Ok(eventCount);
    }

    // GET: api/venues/search
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<VenueDto>>> SearchVenues([FromQuery] string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            return BadRequest("Search term is required");
        }

        var venues = await _venueRepository.SearchVenuesAsync(term);

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
}