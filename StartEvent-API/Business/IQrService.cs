using StartEvent_API.Data.Entities;

namespace StartEvent_API.Business
{
    public interface IQrService
    {
        Task<QrGenerationResult> GenerateQrCodeAsync(Guid ticketId, string customerId);
        Task<QrValidationResult> ValidateQrCodeAsync(string ticketCode);
        Task<byte[]?> GetQrCodeImageAsync(string ticketCode);
    }

    public class QrGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? TicketId { get; set; }
        public string? TicketCode { get; set; }
        public string? QrCodePath { get; set; }
        public string? QrCodeBase64 { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class QrValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public Ticket? Ticket { get; set; }
        public string? TicketCode { get; set; }
    }
}
