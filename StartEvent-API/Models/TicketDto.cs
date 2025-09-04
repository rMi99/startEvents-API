using System;

namespace StartEvent_API.Models
{
    public class TicketDto
    {
        public Guid Id { get; set; }
        public string? CustomerId { get; set; }
        public Guid EventId { get; set; }
        public Guid EventPriceId { get; set; }
        public string? TicketNumber { get; set; }
        public string? TicketCode { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public bool IsPaid { get; set; }
        public string? QrCodePath { get; set; }
        public int PointsEarned { get; set; }
        public int PointsRedeemed { get; set; }
    }
}