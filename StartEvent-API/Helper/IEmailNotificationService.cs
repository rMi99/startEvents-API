using StartEvent_API.Data.Entities;

namespace StartEvent_API.Helper
{
    public interface IEmailNotificationService
    {
        Task<bool> SendTicketConfirmationAsync(Ticket ticket, string qrCodePath);
        Task<bool> SendQrCodeEmailAsync(Ticket ticket, string qrCodePath, string qrCodeBase64);
    }
}
