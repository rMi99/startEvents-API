using Microsoft.AspNetCore.Identity.UI.Services;

namespace StartEvent_API.Data.Seeders
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For development/testing purposes, just log the email instead of sending it
            // In production, you would implement actual email sending logic here
            Console.WriteLine($"Sending email to: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {htmlMessage}");
            
            // Return a completed task since we're not actually sending emails
            return Task.CompletedTask;
        }
    }
}
