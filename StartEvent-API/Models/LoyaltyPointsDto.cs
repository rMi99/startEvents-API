using System;

namespace StartEvent_API.Models
{
    /// <summary>
    /// LoyaltyPoints model class as specified in requirements
    /// Represents loyalty points transactions for customers
    /// </summary>
    public class LoyaltyPointsDto
    {
        /// <summary>
        /// Unique identifier for the loyalty points record (PointId in requirements)
        /// </summary>
        public Guid PointId { get; set; }

        /// <summary>
        /// User identifier (UserId in requirements)
        /// </summary>
        public string UserId { get; set; } = default!;

        /// <summary>
        /// Points earned in this transaction
        /// </summary>
        public int PointsEarned { get; set; }

        /// <summary>
        /// Points redeemed in this transaction
        /// </summary>
        public int PointsRedeemed { get; set; }

        /// <summary>
        /// Current balance after this transaction
        /// </summary>
        public int Balance { get; set; }

        /// <summary>
        /// When this loyalty points record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Description of the transaction
        /// </summary>
        public string Description { get; set; } = default!;

        /// <summary>
        /// Transaction type for display purposes
        /// </summary>
        public string TransactionType => PointsEarned > 0 ? "Earned" : "Redeemed";

        /// <summary>
        /// Net points for this transaction (positive for earned, negative for redeemed)
        /// </summary>
        public int NetPoints => PointsEarned - PointsRedeemed;
    }

    /// <summary>
    /// Request model for redeeming loyalty points
    /// </summary>
    public class RedeemLoyaltyPointsRequest
    {
        /// <summary>
        /// Number of points to redeem
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Optional description for the redemption
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Ticket ID if redeeming for a specific ticket
        /// </summary>
        public Guid? TicketId { get; set; }
    }

    /// <summary>
    /// Response model for loyalty points operations
    /// </summary>
    public class LoyaltyPointsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public int CurrentBalance { get; set; }
        public int PointsProcessed { get; set; }
        public decimal DiscountValue { get; set; }
    }

    /// <summary>
    /// Model for calculating loyalty points
    /// </summary>
    public class LoyaltyPointsCalculation
    {
        /// <summary>
        /// Purchase amount used for calculation
        /// </summary>
        public decimal PurchaseAmount { get; set; }

        /// <summary>
        /// Points earned (10% of purchase amount)
        /// </summary>
        public int PointsEarned { get; set; }

        /// <summary>
        /// Points to be redeemed
        /// </summary>
        public int PointsToRedeem { get; set; }

        /// <summary>
        /// Discount value from redeemed points
        /// </summary>
        public decimal DiscountValue { get; set; }

        /// <summary>
        /// Final total: (Quantity × UnitPrice) – LoyaltyPointsRedeemed
        /// </summary>
        public decimal FinalTotal { get; set; }

        /// <summary>
        /// Original subtotal before any discounts
        /// </summary>
        public decimal Subtotal { get; set; }
    }
}
