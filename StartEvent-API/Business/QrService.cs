using StartEvent_API.Data.Entities;
using StartEvent_API.Helper;
using StartEvent_API.Repositories;

namespace StartEvent_API.Business
{
    public class QrService : IQrService
    {
        private readonly IQrRepository _qrRepository;
        private readonly IFileStorage _fileStorage;
        private readonly IQrCodeGenerator _qrCodeGenerator;
        private readonly IEmailNotificationService _emailService;

        public QrService(
            IQrRepository qrRepository,
            IFileStorage fileStorage,
            IQrCodeGenerator qrCodeGenerator,
            IEmailNotificationService emailService)
        {
            _qrRepository = qrRepository;
            _fileStorage = fileStorage;
            _qrCodeGenerator = qrCodeGenerator;
            _emailService = emailService;
        }

        public async Task<QrGenerationResult> GenerateQrCodeAsync(Guid ticketId, string customerId)
        {
            try
            {
                // Validate ticket exists and belongs to customer
                var ticket = await _qrRepository.GetTicketByIdAsync(ticketId);
                if (ticket == null)
                {
                    return new QrGenerationResult
                    {
                        Success = false,
                        Message = "Ticket not found",
                        Errors = new List<string> { "Ticket not found" }
                    };
                }

                if (ticket.CustomerId != customerId)
                {
                    return new QrGenerationResult
                    {
                        Success = false,
                        Message = "Access denied",
                        Errors = new List<string> { "You can only generate QR codes for your own tickets" }
                    };
                }

                if (!ticket.IsPaid)
                {
                    return new QrGenerationResult
                    {
                        Success = false,
                        Message = "Ticket not paid",
                        Errors = new List<string> { "Ticket must be paid before generating QR code" }
                    };
                }

                // Generate unique ticket code if not exists
                if (string.IsNullOrEmpty(ticket.TicketCode))
                {
                    ticket.TicketCode = await _qrRepository.GenerateUniqueTicketCodeAsync();
                }

                // Generate QR code data
                var qrData = $"TICKET:{ticket.TicketCode}|EVENT:{ticket.EventId}|CUSTOMER:{ticket.CustomerId}";
                
                // Generate QR code image
                var qrCodeBytes = await _qrCodeGenerator.GenerateQrCodeAsync(qrData, 200);
                var qrCodeBase64 = await _qrCodeGenerator.GenerateQrCodeBase64Async(qrData, 200);

                // Save QR code to file storage
                var fileName = $"{ticket.TicketCode}.png";
                var qrCodePath = await _fileStorage.SaveFileAsync(qrCodeBytes, fileName);

                // Update ticket with QR code path
                ticket.QrCodePath = qrCodePath;
                await _qrRepository.UpdateTicketAsync(ticket);

                // Send email notification
                await _emailService.SendQrCodeEmailAsync(ticket, qrCodePath, qrCodeBase64);

                return new QrGenerationResult
                {
                    Success = true,
                    Message = "QR code generated successfully",
                    TicketId = ticket.Id,
                    TicketCode = ticket.TicketCode,
                    QrCodePath = qrCodePath,
                    QrCodeBase64 = qrCodeBase64
                };
            }
            catch (Exception ex)
            {
                return new QrGenerationResult
                {
                    Success = false,
                    Message = "An error occurred while generating QR code",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<QrValidationResult> ValidateQrCodeAsync(string ticketCode)
        {
            try
            {
                var ticket = await _qrRepository.GetTicketByCodeAsync(ticketCode);
                
                if (ticket == null)
                {
                    return new QrValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid ticket code",
                        TicketCode = ticketCode
                    };
                }

                if (!ticket.IsPaid)
                {
                    return new QrValidationResult
                    {
                        IsValid = false,
                        Message = "Ticket not paid",
                        Ticket = ticket,
                        TicketCode = ticketCode
                    };
                }

                return new QrValidationResult
                {
                    IsValid = true,
                    Message = "Valid ticket",
                    Ticket = ticket,
                    TicketCode = ticketCode
                };
            }
            catch (Exception ex)
            {
                return new QrValidationResult
                {
                    IsValid = false,
                    Message = $"Error validating ticket: {ex.Message}",
                    TicketCode = ticketCode
                };
            }
        }

        public async Task<byte[]?> GetQrCodeImageAsync(string ticketCode)
        {
            try
            {
                var ticket = await _qrRepository.GetTicketByCodeAsync(ticketCode);
                if (ticket == null || string.IsNullOrEmpty(ticket.QrCodePath))
                    return null;

                return await _fileStorage.GetFileAsync(ticket.QrCodePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
