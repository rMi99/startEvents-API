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
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetOrganizerReport(string organizerId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Organizers can only view their own reports
                if (currentUserId != organizerId)
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

        #region Enhanced Organizer Reports

        /// <summary>
        /// Get comprehensive sales report for the logged-in organizer
        /// </summary>
        [HttpGet("my-sales")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetMySalesReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var organizerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(organizerId))
                    return Unauthorized("User ID not found in token");

                var salesReport = await _reportService.GetOrganizerSalesReportAsync(organizerId, startDate, endDate);

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrganizerId = organizerId,
                        Period = new { StartDate = startDate, EndDate = endDate },
                        Report = salesReport,
                        GeneratedAt = DateTime.UtcNow
                    },
                    Message = "Sales report generated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating sales report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get revenue breakdown by month for the logged-in organizer
        /// </summary>
        [HttpGet("my-revenue/monthly/{year:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetMyMonthlyRevenue(int year)
        {
            try
            {
                var organizerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(organizerId))
                    return Unauthorized("User ID not found in token");

                var revenueByMonth = await _reportService.GetOrganizerRevenueByMonthAsync(organizerId, year);

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrganizerId = organizerId,
                        Year = year,
                        RevenueByMonth = revenueByMonth,
                        TotalRevenue = revenueByMonth.Values.Sum(),
                        GeneratedAt = DateTime.UtcNow
                    },
                    Message = "Monthly revenue report generated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating monthly revenue report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get event performance metrics for the logged-in organizer
        /// </summary>
        [HttpGet("my-events/performance")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetMyEventPerformance([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var organizerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(organizerId))
                    return Unauthorized("User ID not found in token");

                var eventPerformance = await _reportService.GetOrganizerEventPerformanceAsync(organizerId, startDate, endDate);

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrganizerId = organizerId,
                        Period = new { StartDate = startDate, EndDate = endDate },
                        Events = eventPerformance,
                        TotalEvents = eventPerformance.Count,
                        TotalRevenue = eventPerformance.Sum(e => e.Revenue),
                        TotalTicketsSold = eventPerformance.Sum(e => e.TicketsSold),
                        GeneratedAt = DateTime.UtcNow
                    },
                    Message = "Event performance report generated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating event performance report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get sales breakdown by period (daily, weekly, monthly) for the logged-in organizer
        /// </summary>
        [HttpGet("my-sales/by-period")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetMySalesByPeriod([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string periodType = "monthly")
        {
            try
            {
                var organizerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(organizerId))
                    return Unauthorized("User ID not found in token");

                var validPeriodTypes = new[] { "daily", "weekly", "monthly" };
                if (!validPeriodTypes.Contains(periodType.ToLower()))
                    return BadRequest("Invalid period type. Valid values: daily, weekly, monthly");

                var salesByPeriod = await _reportService.GetOrganizerSalesByPeriodAsync(organizerId, startDate, endDate, periodType);

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrganizerId = organizerId,
                        Period = new { StartDate = startDate, EndDate = endDate },
                        PeriodType = periodType,
                        SalesByPeriod = salesByPeriod,
                        TotalRevenue = salesByPeriod.Sum(s => s.Revenue),
                        TotalTicketsSold = salesByPeriod.Sum(s => s.TicketsSold),
                        TotalTransactions = salesByPeriod.Sum(s => s.Transactions),
                        GeneratedAt = DateTime.UtcNow
                    },
                    Message = $"Sales by {periodType} report generated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating sales by period report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get payment methods breakdown for the logged-in organizer
        /// </summary>
        [HttpGet("my-payments/methods")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetMyPaymentMethods([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var organizerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(organizerId))
                    return Unauthorized("User ID not found in token");

                var paymentMethods = await _reportService.GetOrganizerPaymentMethodsAsync(organizerId, startDate, endDate);

                var totalAmount = paymentMethods.CardPayments + paymentMethods.CashPayments + paymentMethods.OnlinePayments;
                var totalTransactions = paymentMethods.CardTransactions + paymentMethods.CashTransactions + paymentMethods.OnlineTransactions;

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrganizerId = organizerId,
                        Period = new { StartDate = startDate, EndDate = endDate },
                        PaymentMethods = paymentMethods,
                        Summary = new
                        {
                            TotalAmount = totalAmount,
                            TotalTransactions = totalTransactions,
                            CardPercentage = totalAmount > 0 ? Math.Round((paymentMethods.CardPayments / totalAmount) * 100, 2) : 0,
                            CashPercentage = totalAmount > 0 ? Math.Round((paymentMethods.CashPayments / totalAmount) * 100, 2) : 0,
                            OnlinePercentage = totalAmount > 0 ? Math.Round((paymentMethods.OnlinePayments / totalAmount) * 100, 2) : 0
                        },
                        GeneratedAt = DateTime.UtcNow
                    },
                    Message = "Payment methods report generated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating payment methods report", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get event summary dashboard for the logged-in organizer
        /// </summary>
        [HttpGet("my-events/summary")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetMyEventSummary()
        {
            try
            {
                var organizerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(organizerId))
                    return Unauthorized("User ID not found in token");

                var eventSummary = await _reportService.GetOrganizerEventSummaryAsync(organizerId);
                var currentYear = DateTime.UtcNow.Year;
                var monthlyRevenue = await _reportService.GetOrganizerRevenueByMonthAsync(organizerId, currentYear);

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrganizerId = organizerId,
                        EventSummary = eventSummary,
                        Statistics = new
                        {
                            TotalEvents = eventSummary.Count,
                            PublishedEvents = eventSummary.Count(e => e.Status != "Draft"),
                            UpcomingEvents = eventSummary.Count(e => e.Status == "Upcoming"),
                            PastEvents = eventSummary.Count(e => e.Status == "Past"),
                            TotalRevenue = eventSummary.Sum(e => e.Revenue),
                            TotalTicketsSold = eventSummary.Sum(e => e.TicketsSold),
                            AverageCapacityUtilization = eventSummary.Count > 0 ? Math.Round(eventSummary.Average(e => e.SalesPercentage), 2) : 0
                        },
                        MonthlyRevenue = monthlyRevenue,
                        GeneratedAt = DateTime.UtcNow
                    },
                    Message = "Event summary dashboard generated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while generating event summary", Error = ex.Message });
            }
        }

        #endregion
    }
}
