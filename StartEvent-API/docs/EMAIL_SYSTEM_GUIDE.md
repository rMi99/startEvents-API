# StartEvent API Email System Guide

## Overview

A comprehensive email system has been implemented for the StartEvent API using Brevo as the email service provider. The system supports multiple email types with HTML templates, plain text fallbacks, and proper error handling.

## Features

- **Brevo Integration**: Uses Brevo's API for reliable email delivery
- **Multiple Email Types**: Welcome, ticket confirmation, payment confirmation, reminders, and more
- **HTML Templates**: Beautiful responsive HTML emails with CSS styling
- **Plain Text Fallbacks**: Automatic plain text generation for better compatibility
- **Reusable Design**: Template-based system for easy customization
- **Error Handling**: Comprehensive error handling with retry logic
- **Backend Only**: Not exposed to public, only available within backend APIs

## Configuration

### 1. Brevo API Setup
1. Create a Brevo account at [brevo.com](https://brevo.com)
2. Generate an API key from your Brevo dashboard
3. Update `appsettings.json` with your Brevo API key:

```json
{
  "Email": {
    "BrevoSettings": {
      "ApiKey": "YOUR_BREVO_API_KEY_HERE"
    }
  }
}
```

### 2. Configuration Options

The email system can be configured through `appsettings.json`:

```json
{
  "Email": {
    "BrevoSettings": {
      "ApiKey": "YOUR_BREVO_API_KEY_HERE",
      "ApiUrl": "https://api.brevo.com/v3/smtp/email",
      "SenderName": "StartEvent Team",
      "SenderEmail": "noreply@startevent.com",
      "TimeoutMinutes": 5,
      "MaxRetryAttempts": 3,
      "RetryDelaySeconds": 2
    },
    "GeneralSettings": {
      "ReplyToEmail": "support@startevent.com",
      "EnableEmailLogging": true,
      "EnableBulkEmails": true,
      "MaxBulkEmailSize": 500
    },
    "TemplateSettings": {
      "DefaultLanguage": "en",
      "EnableHtmlEmails": true,
      "EnablePlainTextFallback": true,
      "AttachQrCode": true,
      "AttachTicketPdf": false,
      "IncludeEventImages": true
    },
    "BrandInfo": {
      "CompanyName": "StartEvent",
      "SupportEmail": "support@startevent.com",
      "WebsiteUrl": "https://startevent.com",
      "LogoUrl": "https://startevent.com/assets/logo.png",
      "PrimaryColor": "#007bff",
      "SecondaryColor": "#6c757d",
      "SocialLinks": {
        "Facebook": "https://facebook.com/startevent",
        "Twitter": "https://twitter.com/startevent",
        "Instagram": "https://instagram.com/startevent"
      }
    }
  }
}
```

## Email Types Available

### 1. Welcome Email
Sent when a user registers:
```csharp
await _emailService.SendWelcomeEmailAsync(new WelcomeEmailTemplate
{
    To = new EmailAddress { Email = user.Email, Name = user.Name },
    UserName = user.Name,
    VerificationLink = verificationUrl
});
```

### 2. Ticket Confirmation Email
Sent when a ticket is purchased:
```csharp
await _emailService.SendTicketConfirmationEmailAsync(new TicketConfirmationEmailTemplate
{
    To = new EmailAddress { Email = user.Email, Name = user.Name },
    Event = eventDetails,
    Venue = venue,
    Ticket = ticket,
    QrCodeBase64 = qrCodeData,
    TicketDownloadLink = downloadUrl
});
```

### 3. Payment Confirmation Email
Sent after successful payment:
```csharp
await _emailService.SendPaymentConfirmationEmailAsync(new PaymentConfirmationEmailTemplate
{
    To = new EmailAddress { Email = user.Email, Name = user.Name },
    Payment = paymentDetails,
    Event = eventDetails,
    Ticket = ticket,
    ReceiptDownloadLink = receiptUrl
});
```

## Current Integration Points

### 1. Authentication Service (`AuthService.cs`)
- **Welcome Email**: Automatically sent when a user registers
- **Integration**: Added to `RegisterAsync` method

### 2. Payment Controller (`PaymentController.cs`)
- **Ticket Confirmation Email**: Sent after successful payment
- **Payment Confirmation Email**: Sent for payment records
- **Integration**: Added to `ProcessSuccessfulPayment` method

## Code Structure

### Models
- `EmailConfiguration.cs` - Configuration classes
- `EmailTemplates.cs` - Template models for all email types
- `BrevoModels.cs` - Brevo API request/response models

### Services
- `IEmailService.cs` - Email service interface
- `BrevoEmailService.cs` - Brevo implementation
- `IEmailTemplateService.cs` - Template service interface
- `EmailTemplateService.cs` - HTML template generation

### Key Features
- **Dependency Injection**: Properly registered in `Program.cs`
- **Error Handling**: Comprehensive error handling and logging
- **Retry Logic**: Automatic retries for failed email sends
- **Template System**: Reusable HTML templates with CSS styling
- **Plain Text Support**: Automatic plain text generation

## Usage Examples

### Sending a Custom Email
```csharp
public class YourController : ControllerBase
{
    private readonly IEmailService _emailService;

    public YourController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-reminder")]
    public async Task<IActionResult> SendReminder()
    {
        var template = new EventReminderTemplate
        {
            To = new EmailAddress { Email = "user@example.com", Name = "John Doe" },
            Event = eventDetails,
            Venue = venueDetails,
            TimeUntilEvent = "2 hours"
        };

        var result = await _emailService.SendEventReminderEmailAsync(template);
        
        if (result.Success)
            return Ok("Reminder sent successfully");
        else
            return BadRequest($"Failed to send reminder: {result.ErrorMessage}");
    }
}
```

### Bulk Email Sending
```csharp
var templates = new List<WelcomeEmailTemplate>
{
    new WelcomeEmailTemplate { /* template data */ },
    new WelcomeEmailTemplate { /* template data */ },
    // ... more templates
};

var bulkResult = await _emailService.SendBulkEmailsAsync(templates);
Console.WriteLine($"Sent {bulkResult.SuccessfulEmails} out of {bulkResult.TotalEmails} emails");
```

## Testing

To test the email system:

1. **Update Configuration**: Add your Brevo API key to `appsettings.json`
2. **Register a User**: Use the registration endpoint to trigger a welcome email
3. **Purchase a Ticket**: Complete a payment to trigger ticket/payment confirmation emails
4. **Check Logs**: Monitor application logs for email sending status

## Security Notes

- **Backend Only**: Email services are not exposed through public APIs
- **API Key Security**: Store Brevo API key securely in configuration
- **Email Validation**: All email addresses are validated before sending
- **Rate Limiting**: Built-in retry logic prevents API abuse

## Troubleshooting

### Common Issues

1. **Configuration Errors**: Ensure Brevo API key is valid and properly configured
2. **Template Errors**: Check that all required template properties are populated
3. **Network Issues**: Verify internet connectivity and Brevo API availability
4. **Email Delivery**: Check spam folders and verify recipient email addresses

### Logs

The system provides comprehensive logging:
- Email sending attempts
- Success/failure status
- Error messages and stack traces
- Template generation status

Monitor logs for troubleshooting email delivery issues.

## Next Steps

The email system is now fully implemented and ready for use. To extend functionality:

1. **Add New Email Types**: Create new template classes and add methods to the email service
2. **Customize Templates**: Modify the HTML templates in `EmailTemplateService.cs`
3. **Add Attachments**: Extend templates to support file attachments
4. **Scheduled Emails**: Integrate with a job scheduler for automated email campaigns

The system is designed to be easily extensible and maintainable for future enhancements.