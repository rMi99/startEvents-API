using StartEvent_API.Data.Entities;

namespace StartEvent_API.Repositories
{
    public interface IQrRepository
    {
        Task<Ticket?> GetTicketByIdAsync(Guid ticketId);
        Task<Ticket?> GetTicketByCodeAsync(string ticketCode);
        Task<Ticket> UpdateTicketAsync(Ticket ticket);
        Task<bool> TicketExistsAsync(Guid ticketId);
        Task<bool> IsTicketValidAsync(Guid ticketId);
        Task<string> GenerateUniqueTicketCodeAsync();
    }
}
