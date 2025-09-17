using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models;
using StartEvent_API.Helper;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/admin/organizers")]
    [Authorize(Roles = "Admin")]
    public class AdminOrganizersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileStorage _fileStorage;

        public AdminOrganizersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IFileStorage fileStorage)
        {
            _context = context;
            _userManager = userManager;
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// Gets all organizers with filtering and statistics
        /// </summary>
        /// <param name="search">Search in organizer name, email, or organization</param>
        /// <param name="isActive">Filter by active status</param>
        /// <param name="emailConfirmed">Filter by email confirmation status</param>
        /// <param name="hasEvents">Filter organizers who have created events</param>
        /// <param name="organizationName">Filter by organization name</param>
        /// <returns>List of organizers with their statistics</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllOrganizers(
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? emailConfirmed = null,
            [FromQuery] bool? hasEvents = null,
            [FromQuery] string? organizationName = null)
        {
            // Get all users who have the "Organizer" role
            var organizerRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Organizer");

            if (organizerRole == null)
            {
                return BadRequest("Organizer role not found");
            }

            var organizerUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == organizerRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var query = _context.Users
                .Where(u => organizerUserIds.Contains(u.Id))
                .Include(u => u.OrganizedEvents)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.OrganizationName != null && u.OrganizationName.Contains(search)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (emailConfirmed.HasValue)
            {
                query = query.Where(u => u.EmailConfirmed == emailConfirmed.Value);
            }

            if (!string.IsNullOrEmpty(organizationName))
            {
                query = query.Where(u => u.OrganizationName != null && u.OrganizationName.Contains(organizationName));
            }

            var organizers = await query.OrderBy(u => u.FullName).ToListAsync();

            // Apply hasEvents filter after loading (since it requires complex logic)
            if (hasEvents.HasValue)
            {
                if (hasEvents.Value)
                {
                    organizers = organizers.Where(u => u.OrganizedEvents?.Any() == true).ToList();
                }
                else
                {
                    organizers = organizers.Where(u => u.OrganizedEvents?.Any() != true).ToList();
                }
            }

            var organizerDtos = organizers.Select(u => new OrganizerDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                UserName = u.UserName,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                DateOfBirth = u.DateOfBirth,
                OrganizationName = u.OrganizationName,
                OrganizationContact = u.OrganizationContact,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                TotalEvents = u.OrganizedEvents?.Count ?? 0,
                PublishedEvents = u.OrganizedEvents?.Count(e => e.IsPublished) ?? 0,
                UnpublishedEvents = u.OrganizedEvents?.Count(e => !e.IsPublished) ?? 0,
                UpcomingEvents = u.OrganizedEvents?.Count(e => e.EventDate >= DateTime.UtcNow) ?? 0,
                PastEvents = u.OrganizedEvents?.Count(e => e.EventDate < DateTime.UtcNow) ?? 0
            }).ToList();

            return Ok(organizerDtos);
        }

        /// <summary>
        /// Gets detailed information about a specific organizer
        /// </summary>
        /// <param name="id">Organizer ID</param>
        /// <returns>Detailed organizer information with recent events</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrganizerDetails(string id)
        {
            // Verify the user is an organizer
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Organizer not found");
            }

            var isOrganizer = await _userManager.IsInRoleAsync(user, "Organizer");
            if (!isOrganizer)
            {
                return BadRequest("User is not an organizer");
            }

            // Get organizer with events
            var organizerWithEvents = await _context.Users
                .Where(u => u.Id == id)
                .Include(u => u.OrganizedEvents!)
                .ThenInclude(e => e.Venue)
                .FirstOrDefaultAsync();

            if (organizerWithEvents == null)
            {
                return NotFound("Organizer not found");
            }

            // Get recent events (last 10)
            var recentEvents = organizerWithEvents.OrganizedEvents?
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .Select(e => new AdminEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    EventTime = e.EventTime,
                    Category = e.Category,
                    Image = e.Image,
                    ImageUrl = !string.IsNullOrEmpty(e.Image) ? _fileStorage.GetFileUrlAsync(e.Image).Result : null,
                    IsPublished = e.IsPublished,
                    VenueId = e.VenueId,
                    VenueName = e.Venue?.Name ?? string.Empty,
                    OrganizerId = e.OrganizerId,
                    OrganizerName = organizerWithEvents.FullName,
                    OrganizerEmail = organizerWithEvents.Email,
                    OrganizationName = organizerWithEvents.OrganizationName,
                    CreatedAt = e.CreatedAt,
                    ModifiedAt = e.ModifiedAt
                }).ToList() ?? new List<AdminEventDto>();

            // Get unique categories
            var eventCategories = organizerWithEvents.OrganizedEvents?
                .Where(e => !string.IsNullOrEmpty(e.Category))
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList() ?? new List<string>();

            // Get first and last event dates
            var firstEventDate = organizerWithEvents.OrganizedEvents?
                .OrderBy(e => e.CreatedAt)
                .FirstOrDefault()?.CreatedAt;

            var lastEventDate = organizerWithEvents.OrganizedEvents?
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault()?.CreatedAt;

            var organizerDetailDto = new OrganizerDetailDto
            {
                Id = organizerWithEvents.Id,
                FullName = organizerWithEvents.FullName,
                Email = organizerWithEvents.Email ?? string.Empty,
                UserName = organizerWithEvents.UserName,
                PhoneNumber = organizerWithEvents.PhoneNumber,
                Address = organizerWithEvents.Address,
                DateOfBirth = organizerWithEvents.DateOfBirth,
                OrganizationName = organizerWithEvents.OrganizationName,
                OrganizationContact = organizerWithEvents.OrganizationContact,
                CreatedAt = organizerWithEvents.CreatedAt,
                LastLogin = organizerWithEvents.LastLogin,
                IsActive = organizerWithEvents.IsActive,
                EmailConfirmed = organizerWithEvents.EmailConfirmed,
                PhoneNumberConfirmed = organizerWithEvents.PhoneNumberConfirmed,
                TotalEvents = organizerWithEvents.OrganizedEvents?.Count ?? 0,
                PublishedEvents = organizerWithEvents.OrganizedEvents?.Count(e => e.IsPublished) ?? 0,
                UnpublishedEvents = organizerWithEvents.OrganizedEvents?.Count(e => !e.IsPublished) ?? 0,
                UpcomingEvents = organizerWithEvents.OrganizedEvents?.Count(e => e.EventDate >= DateTime.UtcNow) ?? 0,
                PastEvents = organizerWithEvents.OrganizedEvents?.Count(e => e.EventDate < DateTime.UtcNow) ?? 0,
                RecentEvents = recentEvents,
                EventCategories = eventCategories,
                FirstEventDate = firstEventDate,
                LastEventDate = lastEventDate
            };

            return Ok(organizerDetailDto);
        }

        /// <summary>
        /// Updates organizer active status (Admin can activate/deactivate organizers)
        /// </summary>
        /// <param name="id">Organizer ID</param>
        /// <param name="isActive">New active status</param>
        /// <returns>Updated organizer information</returns>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrganizerStatus(string id, [FromBody] bool isActive)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Organizer not found");
            }

            var isOrganizer = await _userManager.IsInRoleAsync(user, "Organizer");
            if (!isOrganizer)
            {
                return BadRequest("User is not an organizer");
            }

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest("Failed to update organizer status");
            }

            // Return updated organizer info
            var organizerDto = new OrganizerDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                OrganizationName = user.OrganizationName,
                OrganizationContact = user.OrganizationContact,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TotalEvents = 0, // We don't load events for this endpoint
                PublishedEvents = 0,
                UnpublishedEvents = 0,
                UpcomingEvents = 0,
                PastEvents = 0
            };

            return Ok(organizerDto);
        }

        /// <summary>
        /// Gets organizer statistics for admin dashboard
        /// </summary>
        /// <returns>Organizer statistics</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetOrganizerStatistics()
        {
            // Get all organizers
            var organizerRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Organizer");

            if (organizerRole == null)
            {
                return BadRequest("Organizer role not found");
            }

            var organizerUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == organizerRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var organizers = await _context.Users
                .Where(u => organizerUserIds.Contains(u.Id))
                .Include(u => u.OrganizedEvents)
                .ToListAsync();

            var stats = new
            {
                TotalOrganizers = organizers.Count,
                ActiveOrganizers = organizers.Count(u => u.IsActive),
                InactiveOrganizers = organizers.Count(u => !u.IsActive),
                VerifiedOrganizers = organizers.Count(u => u.EmailConfirmed),
                UnverifiedOrganizers = organizers.Count(u => !u.EmailConfirmed),
                OrganizersWithEvents = organizers.Count(u => u.OrganizedEvents?.Any() == true),
                OrganizersWithoutEvents = organizers.Count(u => u.OrganizedEvents?.Any() != true),
                TopOrganizers = organizers
                    .Where(u => u.OrganizedEvents?.Any() == true)
                    .OrderByDescending(u => u.OrganizedEvents!.Count)
                    .Take(5)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Name = u.FullName,
                        Email = u.Email,
                        OrganizationName = u.OrganizationName,
                        EventCount = u.OrganizedEvents!.Count,
                        PublishedEvents = u.OrganizedEvents!.Count(e => e.IsPublished)
                    })
                    .ToList(),
                RecentRegistrations = organizers
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Name = u.FullName,
                        Email = u.Email,
                        CreatedAt = u.CreatedAt,
                        IsActive = u.IsActive,
                        EventCount = u.OrganizedEvents?.Count ?? 0
                    })
                    .ToList()
            };

            return Ok(stats);
        }
    }
}