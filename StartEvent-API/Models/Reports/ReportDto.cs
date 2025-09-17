namespace StartEvent_API.Models.Reports
{
    public class SalesReportDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTicketPrice { get; set; }
        public List<SalesByPeriod> SalesByPeriod { get; set; } = new();
        public List<SalesByEvent> TopEventsBySales { get; set; } = new();
        public List<SalesByCategory> SalesByCategory { get; set; } = new();
        public List<SalesByOrganizer> SalesByOrganizer { get; set; } = new();
        public PaymentMethodBreakdown PaymentMethods { get; set; } = new();
    }

    public class SalesByPeriod
    {
        public string Period { get; set; } = default!; // "2025-01", "2025-09-15", etc.
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public int Transactions { get; set; }
    }

    public class SalesByEvent
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = default!;
        public string EventCategory { get; set; } = default!;
        public DateTime EventDate { get; set; }
        public string OrganizerName { get; set; } = default!;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public int Transactions { get; set; }
    }

    public class SalesByCategory
    {
        public string Category { get; set; } = default!;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public int EventCount { get; set; }
        public decimal AverageTicketPrice { get; set; }
    }

    public class SalesByOrganizer
    {
        public string OrganizerId { get; set; } = default!;
        public string OrganizerName { get; set; } = default!;
        public string? OrganizationName { get; set; }
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public int EventCount { get; set; }
        public decimal AverageRevenuePerEvent { get; set; }
    }

    public class PaymentMethodBreakdown
    {
        public decimal CardPayments { get; set; }
        public decimal CashPayments { get; set; }
        public decimal OnlinePayments { get; set; }
        public int CardTransactions { get; set; }
        public int CashTransactions { get; set; }
        public int OnlineTransactions { get; set; }
    }

    public class UserReportDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int UnverifiedUsers { get; set; }
        public int Organizers { get; set; }
        public int Customers { get; set; }
        public int Admins { get; set; }
        public List<UserRegistrationTrend> RegistrationTrend { get; set; } = new();
        public List<UserActivitySummary> MostActiveUsers { get; set; } = new();
        public List<OrganizerPerformance> TopOrganizers { get; set; } = new();
    }

    public class UserRegistrationTrend
    {
        public string Period { get; set; } = default!;
        public int NewRegistrations { get; set; }
        public int NewOrganizers { get; set; }
        public int NewCustomers { get; set; }
    }

    public class UserActivitySummary
    {
        public string UserId { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime LastLogin { get; set; }
        public int TicketsPurchased { get; set; }
        public decimal TotalSpent { get; set; }
        public int EventsOrganized { get; set; }
    }

    public class OrganizerPerformance
    {
        public string OrganizerId { get; set; } = default!;
        public string OrganizerName { get; set; } = default!;
        public int EventsCreated { get; set; }
        public int PublishedEvents { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TicketsSold { get; set; }
        public double AverageEventRating { get; set; } // If you have ratings
    }

    public class EventReportDto
    {
        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int UnpublishedEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int PastEvents { get; set; }
        public int OngoingEvents { get; set; }
        public List<EventsByCategory> EventsByCategory { get; set; } = new();
        public List<EventsByPeriod> EventsByPeriod { get; set; } = new();
        public List<PopularEvent> MostPopularEvents { get; set; } = new();
        public List<EventPerformance> EventPerformance { get; set; } = new();
        public VenueUtilization VenueStats { get; set; } = new();
    }

    public class EventsByCategory
    {
        public string Category { get; set; } = default!;
        public int EventCount { get; set; }
        public int PublishedCount { get; set; }
        public decimal AverageTicketsSold { get; set; }
        public decimal AverageRevenue { get; set; }
    }

    public class EventsByPeriod
    {
        public string Period { get; set; } = default!;
        public int EventsCreated { get; set; }
        public int EventsPublished { get; set; }
        public int EventsHeld { get; set; }
    }

    public class PopularEvent
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = default!;
        public string Category { get; set; } = default!;
        public DateTime EventDate { get; set; }
        public string OrganizerName { get; set; } = default!;
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public int Views { get; set; } // If you track views
    }

    public class EventPerformance
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = default!;
        public string Status { get; set; } = default!; // Published, Draft, Past
        public DateTime EventDate { get; set; }
        public int TotalCapacity { get; set; }
        public int TicketsSold { get; set; }
        public decimal SalesPercentage { get; set; }
        public decimal Revenue { get; set; }
    }

    public class VenueUtilization
    {
        public int TotalVenues { get; set; }
        public int ActiveVenues { get; set; }
        public List<VenueUsage> MostUsedVenues { get; set; } = new();
        public List<VenueUsage> LeastUsedVenues { get; set; } = new();
    }

    public class VenueUsage
    {
        public Guid VenueId { get; set; }
        public string VenueName { get; set; } = default!;
        public string Location { get; set; } = default!;
        public int EventCount { get; set; }
        public int UpcomingEventCount { get; set; }
        public decimal UtilizationRate { get; set; }
    }

    public class RevenueReportDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public List<RevenueByPeriod> RevenueByPeriod { get; set; } = new();
        public List<RevenueByEvent> TopRevenueEvents { get; set; } = new();
        public List<RevenueByOrganizer> RevenueByOrganizer { get; set; } = new();
        public List<RevenueByCategory> RevenueByCategory { get; set; } = new();
        public RevenueProjection Projection { get; set; } = new();
    }

    public class RevenueByPeriod
    {
        public string Period { get; set; } = default!;
        public decimal Revenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal GrowthRate { get; set; } // Compared to previous period
    }

    public class RevenueByEvent
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = default!;
        public DateTime EventDate { get; set; }
        public string OrganizerName { get; set; } = default!;
        public decimal Revenue { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal CompletedAmount { get; set; }
        public int TicketsSold { get; set; }
    }

    public class RevenueByOrganizer
    {
        public string OrganizerId { get; set; } = default!;
        public string OrganizerName { get; set; } = default!;
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public int EventCount { get; set; }
        public decimal AverageRevenuePerEvent { get; set; }
    }

    public class RevenueByCategory
    {
        public string Category { get; set; } = default!;
        public decimal Revenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public int EventCount { get; set; }
        public decimal MarketShare { get; set; } // Percentage of total revenue
    }

    public class RevenueProjection
    {
        public decimal ProjectedMonthlyRevenue { get; set; }
        public decimal ProjectedQuarterlyRevenue { get; set; }
        public decimal ProjectedAnnualRevenue { get; set; }
        public decimal GrowthRate { get; set; }
        public string ProjectionBasis { get; set; } = default!; // "Last 3 months average", etc.
    }
}