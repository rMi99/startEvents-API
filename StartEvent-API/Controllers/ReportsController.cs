using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Business;
using StartEvent_API.Data.Entities;
using System.Security.Claims;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Get organizer's ticket sales and revenue report
        /// </summary>
        [HttpGet("organizer/{organizerId}")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> GetOrganizerReport(string organizerId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Organizers can only view their own reports, Admins can view any
                if (userRole != "Admin" && currentUserId != organizerId)
                    return Forbid("You can only view your own reports");

                var ticketSales = await _reportService.GetOrganizerTicketSalesAsync(organizerId, startDate, endDate);
                var revenue = await _reportService.GetOrganizerRevenueAsync(organizerId, startDate, endDate);
                var events = await _reportService.GetOrganizerEventsAsync(organizerId);

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrganizerId = organizerId,
                        Period = new { StartDate = startDate, EndDate = endDate },
                        TicketSales = ticketSales,
                        TotalRevenue = revenue,
                        TotalTicketsSold = ticketSales.Sum(t => t.Quantity),
                        Events = events,
                        GeneratedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating organizer report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get system-wide sales report (Admin only)
        /// </summary>
        [HttpGet("admin/sales")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemWideSalesReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var ticketSales = await _reportService.GetSystemWideTicketSalesAsync(startDate, endDate);
                var revenue = await _reportService.GetSystemWideRevenueAsync(startDate, endDate);
                var revenueByMonth = await _reportService.GetRevenueByMonthAsync(DateTime.UtcNow.Year);
                var salesByCategory = await _reportService.GetTicketSalesByCategoryAsync();

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        Period = new { StartDate = startDate, EndDate = endDate },
                        TicketSales = ticketSales,
                        TotalRevenue = revenue,
                        TotalTicketsSold = ticketSales.Sum(t => t.Quantity),
                        RevenueByMonth = revenueByMonth,
                        SalesByCategory = salesByCategory,
                        GeneratedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating sales report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get system-wide user statistics (Admin only)
        /// </summary>
        [HttpGet("admin/users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var users = await _reportService.GetSystemUsersAsync();
                var userStats = await _reportService.GetUserStatisticsAsync();

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        Users = users.Select(u => new
                        {
                            u.Id,
                            u.Email,
                            u.FullName,
                            u.CreatedAt,
                            u.LastLogin,
                            u.IsActive,
                            u.OrganizationName
                        }),
                        Statistics = userStats,
                        GeneratedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating user statistics", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get all events with performance metrics (Admin only)
        /// </summary>
        [HttpGet("admin/events")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEventsWithMetrics()
        {
            try
            {
                var events = await _reportService.GetAllEventsWithMetricsAsync();

                var eventsWithMetrics = events.Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.EventDate,
                    e.Category,
                    e.IsPublished,
                    e.CreatedAt,
                    Organizer = new { e.Organizer.Id, e.Organizer.Email, e.Organizer.FullName },
                    Venue = new { e.Venue.Id, e.Venue.Name, e.Venue.Location, e.Venue.Capacity },
                    Prices = e.Prices.Select(p => new { p.Id, p.Category, p.Price, p.Stock, p.IsActive }),
                    TicketMetrics = new
                    {
                        TotalTicketsSold = e.Tickets.Where(t => t.IsPaid).Sum(t => t.Quantity),
                        TotalRevenue = e.Tickets.Where(t => t.IsPaid).Sum(t => t.TotalAmount),
                        PendingTickets = e.Tickets.Count(t => !t.IsPaid)
                    }
                });

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        Events = eventsWithMetrics,
                        TotalEvents = events.Count(),
                        GeneratedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating events report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get system monitoring data (Admin only)
        /// </summary>
        [HttpGet("admin/monitoring")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemMonitoring()
        {
            try
            {
                var monitoringData = await _reportService.GetSystemMonitoringDataAsync();

                return Ok(new
                {
                    Success = true,
                    Data = monitoringData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving monitoring data", Error = ex.Message });
            }
        }
    }
}
