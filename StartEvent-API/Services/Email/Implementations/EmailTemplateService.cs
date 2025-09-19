using Microsoft.Extensions.Options;
using StartEvent_API.Models.Email;
using StartEvent_API.Services.Email;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace StartEvent_API.Services.Email.Implementations
{
    /// <summary>
    /// Email template service implementation with HTML generation
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly EmailConfiguration _emailConfig;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(
            IOptions<EmailConfiguration> emailConfig,
            ILogger<EmailTemplateService> logger)
        {
            _emailConfig = emailConfig.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderTemplateAsync<T>(T template) where T : EmailTemplateBase
        {
            var validation = await ValidateTemplateAsync(template);
            if (!validation.IsValid)
            {
                throw new ArgumentException($"Invalid template: {string.Join(", ", validation.Errors)}");
            }

            return template switch
            {
                WelcomeEmailTemplate welcome => await RenderWelcomeEmailAsync(welcome),
                TicketConfirmationEmailTemplate ticket => await RenderTicketConfirmationEmailAsync(ticket),
                PaymentConfirmationEmailTemplate payment => await RenderPaymentConfirmationEmailAsync(payment),
                EventReminderEmailTemplate reminder => await RenderEventReminderEmailAsync(reminder),
                EventCancellationEmailTemplate cancellation => await RenderEventCancellationEmailAsync(cancellation),
                PasswordResetEmailTemplate passwordReset => await RenderPasswordResetEmailAsync(passwordReset),
                EmailVerificationEmailTemplate verification => await RenderEmailVerificationEmailAsync(verification),
                _ => await RenderGenericTemplateAsync(template)
            };
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderWelcomeEmailAsync(WelcomeEmailTemplate template)
        {
            var htmlContent = GenerateWelcomeEmailHtml(template);
            var textContent = GenerateWelcomeEmailText(template);

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = new List<EmailAttachment>()
            });
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderTicketConfirmationEmailAsync(TicketConfirmationEmailTemplate template)
        {
            var htmlContent = GenerateTicketConfirmationEmailHtml(template);
            var textContent = GenerateTicketConfirmationEmailText(template);

            var attachments = new List<EmailAttachment>();

            // Add QR code as attachment if available
            if (!string.IsNullOrEmpty(template.QrCodeBase64))
            {
                var qrCodeBytes = Convert.FromBase64String(template.QrCodeBase64);
                attachments.Add(new EmailAttachment
                {
                    FileName = $"ticket-qr-{template.Ticket.Id}.png",
                    Content = qrCodeBytes,
                    ContentType = "image/png",
                    Disposition = "inline"
                });
            }

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = attachments
            });
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderPaymentConfirmationEmailAsync(PaymentConfirmationEmailTemplate template)
        {
            var htmlContent = GeneratePaymentConfirmationEmailHtml(template);
            var textContent = GeneratePaymentConfirmationEmailText(template);

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = new List<EmailAttachment>()
            });
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderEventReminderEmailAsync(EventReminderEmailTemplate template)
        {
            var htmlContent = GenerateEventReminderEmailHtml(template);
            var textContent = GenerateEventReminderEmailText(template);

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = new List<EmailAttachment>()
            });
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderEventCancellationEmailAsync(EventCancellationEmailTemplate template)
        {
            var htmlContent = GenerateEventCancellationEmailHtml(template);
            var textContent = GenerateEventCancellationEmailText(template);

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = new List<EmailAttachment>()
            });
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderPasswordResetEmailAsync(PasswordResetEmailTemplate template)
        {
            var htmlContent = GeneratePasswordResetEmailHtml(template);
            var textContent = GeneratePasswordResetEmailText(template);

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = new List<EmailAttachment>()
            });
        }

        /// <inheritdoc/>
        public async Task<EmailTemplateResult> RenderEmailVerificationEmailAsync(EmailVerificationEmailTemplate template)
        {
            var htmlContent = GenerateEmailVerificationEmailHtml(template);
            var textContent = GenerateEmailVerificationEmailText(template);

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = new List<EmailAttachment>()
            });
        }

        /// <inheritdoc/>
        public async Task<TemplateValidationResult> ValidateTemplateAsync<T>(T template) where T : EmailTemplateBase
        {
            var result = new TemplateValidationResult { IsValid = true };

            // Common validations
            if (string.IsNullOrWhiteSpace(template.To.Email))
            {
                result.Errors.Add("Recipient email is required");
                result.IsValid = false;
            }
            else if (!IsValidEmail(template.To.Email))
            {
                result.Errors.Add("Recipient email format is invalid");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(template.Subject))
            {
                result.Errors.Add("Subject is required");
                result.IsValid = false;
            }

            // Template-specific validations
            switch (template)
            {
                case TicketConfirmationEmailTemplate ticketTemplate:
                    if (ticketTemplate.Ticket?.Id == null)
                    {
                        result.Errors.Add("Ticket information is required");
                        result.IsValid = false;
                    }
                    if (ticketTemplate.Event?.Id == null)
                    {
                        result.Errors.Add("Event information is required");
                        result.IsValid = false;
                    }
                    break;

                case PaymentConfirmationEmailTemplate paymentTemplate:
                    if (paymentTemplate.Payment?.Id == null)
                    {
                        result.Errors.Add("Payment information is required");
                        result.IsValid = false;
                    }
                    break;

                case PasswordResetEmailTemplate resetTemplate:
                    if (string.IsNullOrWhiteSpace(resetTemplate.ResetLink))
                    {
                        result.Errors.Add("Reset link is required");
                        result.IsValid = false;
                    }
                    break;

                case EmailVerificationEmailTemplate verificationTemplate:
                    if (string.IsNullOrWhiteSpace(verificationTemplate.VerificationLink) &&
                        string.IsNullOrWhiteSpace(verificationTemplate.VerificationCode))
                    {
                        result.Errors.Add("Either verification link or verification code is required");
                        result.IsValid = false;
                    }
                    break;
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Renders a generic template for unspecified types
        /// </summary>
        private async Task<EmailTemplateResult> RenderGenericTemplateAsync<T>(T template) where T : EmailTemplateBase
        {
            var htmlContent = GenerateGenericEmailHtml(template);
            var textContent = GenerateGenericEmailText(template);

            return await Task.FromResult(new EmailTemplateResult
            {
                HtmlContent = htmlContent,
                TextContent = textContent,
                Subject = template.Subject,
                Attachments = new List<EmailAttachment>()
            });
        }

        #region HTML Template Generators

        private string GenerateWelcomeEmailHtml(WelcomeEmailTemplate template)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>Welcome to {_emailConfig.Templates.Brand.CompanyName}!</h1>
            
            <p>Hi {template.User.FullName ?? template.To.Name},</p>
            
            <p>Welcome to {_emailConfig.Templates.Brand.CompanyName}! We're excited to have you join our community of event enthusiasts.</p>
            
            <p>Your account has been successfully created and you can now:</p>
            <ul>
                <li>Discover amazing events</li>
                <li>Purchase tickets easily</li>
                <li>Manage your bookings</li>
                <li>Get personalized recommendations</li>
            </ul>
            
            {(!string.IsNullOrEmpty(template.VerificationLink) ? $@"
            <div class='cta-section'>
                <p>To complete your registration, please verify your email address:</p>
                <a href='{template.VerificationLink}' class='btn btn-primary'>Verify Email Address</a>
            </div>" : "")}
            
            {(!string.IsNullOrEmpty(template.DashboardLink) ? $@"
            <div class='cta-section'>
                <a href='{template.DashboardLink}' class='btn btn-secondary'>Go to Dashboard</a>
            </div>" : "")}
            
            <p>If you have any questions, feel free to contact our support team at {_emailConfig.Templates.Brand.SupportEmail}.</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateTicketConfirmationEmailHtml(TicketConfirmationEmailTemplate template)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>🎫 Your Ticket Confirmation</h1>
            
            <p>Hi {template.To.Name},</p>
            
            <p>Great news! Your ticket for <strong>{template.Event.Title}</strong> has been confirmed.</p>
            
            <div class='ticket-info'>
                <h2>Event Details</h2>
                <table>
                    <tr><td><strong>Event:</strong></td><td>{template.Event.Title}</td></tr>
                    <tr><td><strong>Date & Time:</strong></td><td>{template.Event.EventDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</td></tr>
                    <tr><td><strong>Venue:</strong></td><td>{template.Venue?.Name ?? "TBD"}</td></tr>
                    {(!string.IsNullOrEmpty(template.Venue?.Location) ? $"<tr><td><strong>Address:</strong></td><td>{template.Venue.Location}</td></tr>" : "")}
                    <tr><td><strong>Ticket Type:</strong></td><td>{template.Ticket.EventPrice.Category}</td></tr>
                    <tr><td><strong>Quantity:</strong></td><td>{template.Ticket.Quantity}</td></tr>
                    <tr><td><strong>Total Amount:</strong></td><td>${template.Ticket.TotalAmount:F2}</td></tr>
                </table>
            </div>
            
            {(!string.IsNullOrEmpty(template.QrCodeBase64) ? @"
            <div class='qr-section'>
                <h3>Your Ticket QR Code</h3>
                <p>Present this QR code at the event entrance:</p>
                <img src='cid:ticket-qr' alt='Ticket QR Code' style='max-width: 200px; height: auto;' />
            </div>" : "")}
            
            <div class='cta-section'>
                {(!string.IsNullOrEmpty(template.TicketDownloadLink) ? $"<a href='{template.TicketDownloadLink}' class='btn btn-primary'>Download Ticket</a>" : "")}
                {(!string.IsNullOrEmpty(template.EventDetailsLink) ? $"<a href='{template.EventDetailsLink}' class='btn btn-secondary'>View Event Details</a>" : "")}
            </div>
            
            <div class='important-info'>
                <h3>Important Information</h3>
                <ul>
                    <li>Please arrive 30 minutes before the event starts</li>
                    <li>Bring a valid ID for verification</li>
                    <li>Screenshots of tickets are not accepted - please show the original QR code</li>
                </ul>
            </div>
            
            <p>We can't wait to see you at the event!</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GeneratePaymentConfirmationEmailHtml(PaymentConfirmationEmailTemplate template)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>💳 Payment Confirmation</h1>
            
            <p>Hi {template.To.Name},</p>
            
            <p>Your payment has been successfully processed! Here are the details:</p>
            
            <div class='payment-info'>
                <h2>Payment Details</h2>
                <table>
                    <tr><td><strong>Transaction ID:</strong></td><td>{template.Payment.TransactionId}</td></tr>
                    <tr><td><strong>Amount:</strong></td><td>${template.Payment.Amount:F2}</td></tr>
                    <tr><td><strong>Payment Method:</strong></td><td>{template.Payment.PaymentMethod}</td></tr>
                    <tr><td><strong>Date:</strong></td><td>{template.Payment.PaymentDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</td></tr>
                    <tr><td><strong>Status:</strong></td><td>{template.Payment.Status}</td></tr>
                </table>
            </div>
            
            <div class='event-info'>
                <h2>Event Information</h2>
                <table>
                    <tr><td><strong>Event:</strong></td><td>{template.Event.Title}</td></tr>
                    <tr><td><strong>Date:</strong></td><td>{template.Event.EventDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</td></tr>
                    <tr><td><strong>Ticket Type:</strong></td><td>{template.Ticket.EventPrice.Category}</td></tr>
                    <tr><td><strong>Quantity:</strong></td><td>{template.Ticket.Quantity}</td></tr>
                </table>
            </div>
            
            {(!string.IsNullOrEmpty(template.ReceiptDownloadLink) ? $@"
            <div class='cta-section'>
                <a href='{template.ReceiptDownloadLink}' class='btn btn-primary'>Download Receipt</a>
            </div>" : "")}
            
            <p>Your ticket confirmation will be sent separately. If you have any questions about your payment, please contact our support team.</p>
            
            <p>Thank you for your purchase!</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateEventReminderEmailHtml(EventReminderEmailTemplate template)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>⏰ Event Reminder</h1>
            
            <p>Hi {template.To.Name},</p>
            
            <p>This is a friendly reminder that <strong>{template.Event.Title}</strong> is starting in {template.TimeUntilEvent}!</p>
            
            <div class='event-info'>
                <h2>Event Details</h2>
                <table>
                    <tr><td><strong>Event:</strong></td><td>{template.Event.Title}</td></tr>
                    <tr><td><strong>Date & Time:</strong></td><td>{template.Event.EventDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</td></tr>
                    <tr><td><strong>Venue:</strong></td><td>{template.Venue?.Name ?? "TBD"}</td></tr>
                    {(!string.IsNullOrEmpty(template.Venue?.Location) ? $"<tr><td><strong>Address:</strong></td><td>{template.Venue.Location}</td></tr>" : "")}
                </table>
            </div>
            
            {(template.UserTickets.Any() ? $@"
            <div class='ticket-info'>
                <h3>Your Tickets</h3>
                <ul>
                    {string.Join("", template.UserTickets.Select(t => $"<li>{t.EventPrice.Category} - Quantity: {t.Quantity}</li>"))}
                </ul>
            </div>" : "")}
            
            <div class='cta-section'>
                {(!string.IsNullOrEmpty(template.DirectionsLink) ? $"<a href='{template.DirectionsLink}' class='btn btn-primary'>Get Directions</a>" : "")}
            </div>
            
            <div class='important-info'>
                <h3>Reminders</h3>
                <ul>
                    <li>Arrive 30 minutes early</li>
                    <li>Bring your ticket QR code</li>
                    <li>Bring a valid ID</li>
                    <li>Check for any last-minute updates</li>
                </ul>
            </div>
            
            <p>We're excited to see you there!</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateEventCancellationEmailHtml(EventCancellationEmailTemplate template)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>❌ Event Cancellation Notice</h1>
            
            <p>Hi {template.To.Name},</p>
            
            <p>We regret to inform you that <strong>{template.Event.Title}</strong> scheduled for {template.Event.EventDate:dddd, MMMM dd, yyyy} has been cancelled.</p>
            
            {(!string.IsNullOrEmpty(template.CancellationReason) ? $@"
            <div class='cancellation-reason'>
                <h3>Reason for Cancellation</h3>
                <p>{template.CancellationReason}</p>
            </div>" : "")}
            
            {(template.AffectedTickets.Any() ? $@"
            <div class='affected-tickets'>
                <h3>Your Affected Tickets</h3>
                <ul>
                    {string.Join("", template.AffectedTickets.Select(t => $"<li>{t.EventPrice.Category} - Quantity: {t.Quantity} - Amount: ${t.TotalAmount:F2}</li>"))}
                </ul>
            </div>" : "")}
            
            <div class='refund-info'>
                <h3>Refund Information</h3>
                <table>
                    <tr><td><strong>Refund Amount:</strong></td><td>${template.RefundDetails.RefundAmount:F2}</td></tr>
                    <tr><td><strong>Refund Method:</strong></td><td>{template.RefundDetails.RefundMethod}</td></tr>
                    <tr><td><strong>Processing Time:</strong></td><td>{template.RefundDetails.ProcessingTime}</td></tr>
                    {(!string.IsNullOrEmpty(template.RefundDetails.ReferenceNumber) ? $"<tr><td><strong>Reference Number:</strong></td><td>{template.RefundDetails.ReferenceNumber}</td></tr>" : "")}
                </table>
            </div>
            
            <p>Your refund will be processed automatically. You should see the refund in your account within the specified processing time.</p>
            
            {(!string.IsNullOrEmpty(template.SupportContactInfo) ? $@"
            <p>If you have any questions or concerns, please contact our support team at {template.SupportContactInfo}.</p>" : "")}
            
            <p>We sincerely apologize for any inconvenience this may cause and appreciate your understanding.</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GeneratePasswordResetEmailHtml(PasswordResetEmailTemplate template)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>🔒 Password Reset Request</h1>
            
            <p>Hi {template.User.FullName ?? template.To.Name},</p>
            
            <p>We received a request to reset your password. If you made this request, click the button below to reset your password:</p>
            
            <div class='cta-section'>
                <a href='{template.ResetLink}' class='btn btn-primary'>Reset Your Password</a>
            </div>
            
            <div class='security-info'>
                <h3>Security Information</h3>
                <ul>
                    <li>This link will expire on {template.LinkExpiresAt:dddd, MMMM dd, yyyy 'at' hh:mm tt}</li>
                    <li>Request made from IP: {template.RequestIpAddress}</li>
                    <li>If you didn't request this, please ignore this email</li>
                </ul>
            </div>
            
            <p>If the button above doesn't work, copy and paste this link into your browser:</p>
            <p style='word-break: break-all; font-family: monospace; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{template.ResetLink}</p>
            
            <p>If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.</p>
            
            <p>For security reasons, this link will expire in 24 hours.</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateEmailVerificationEmailHtml(EmailVerificationEmailTemplate template)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>📧 Verify Your Email Address</h1>
            
            <p>Hi {template.User.FullName ?? template.To.Name},</p>
            
            <p>Thank you for signing up with {_emailConfig.Templates.Brand.CompanyName}! To complete your registration, please verify your email address using the verification code below:</p>
            
            {(!string.IsNullOrEmpty(template.VerificationCode) ? $@"
            <div class='verification-code' style='text-align: center; margin: 30px 0;'>
                <p style='font-size: 16px; margin-bottom: 20px; color: #666;'>Your verification code:</p>
                <div style='font-size: 32px; font-weight: bold; text-align: center; padding: 25px; background-color: #f8f9fa; border: 2px dashed #007bff; border-radius: 12px; letter-spacing: 8px; color: #007bff; font-family: monospace;'>{template.VerificationCode}</div>
                <p style='font-size: 14px; margin-top: 15px; color: #666;'>Enter this 6-digit code in the verification form</p>
            </div>" : "")}
            
            {(!string.IsNullOrEmpty(template.VerificationLink) ? $@"
            <div class='cta-section'>
                <a href='{template.VerificationLink}' class='btn btn-primary'>Verify Email Address</a>
            </div>
            
            <p style='font-size: 14px; color: #666;'>If the button above doesn't work, copy and paste this link into your browser:</p>
            <p style='word-break: break-all; font-family: monospace; background-color: #f8f9fa; padding: 10px; border-radius: 4px; font-size: 12px;'>{template.VerificationLink}</p>" : "")}
            
            <p><strong>Important:</strong> This verification code will expire in 15 minutes for security reasons.</p>
            
            <p>If you didn't create an account with us, you can safely ignore this email.</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateGenericEmailHtml<T>(T template) where T : EmailTemplateBase
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{template.Subject}</title>
    {GetEmailStyles()}
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        
        <div class='content'>
            <h1>{template.Subject}</h1>
            
            <p>Hi {template.To.Name},</p>
            
            <p>This is a notification from {_emailConfig.Templates.Brand.CompanyName}.</p>
            
            <p>Template Type: {template.TemplateType}</p>
            
            <p>If you have any questions, please contact our support team at {_emailConfig.Templates.Brand.SupportEmail}.</p>
            
            <p>Best regards,<br>The {_emailConfig.Templates.Brand.CompanyName} Team</p>
        </div>
        
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        #endregion

        #region Text Template Generators

        private string GenerateWelcomeEmailText(WelcomeEmailTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Welcome to {_emailConfig.Templates.Brand.CompanyName}!");
            sb.AppendLine();
            sb.AppendLine($"Hi {template.User.FullName ?? template.To.Name},");
            sb.AppendLine();
            sb.AppendLine($"Welcome to {_emailConfig.Templates.Brand.CompanyName}! We're excited to have you join our community of event enthusiasts.");
            sb.AppendLine();
            sb.AppendLine("Your account has been successfully created and you can now:");
            sb.AppendLine("- Discover amazing events");
            sb.AppendLine("- Purchase tickets easily");
            sb.AppendLine("- Manage your bookings");
            sb.AppendLine("- Get personalized recommendations");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(template.VerificationLink))
            {
                sb.AppendLine("To complete your registration, please verify your email address:");
                sb.AppendLine(template.VerificationLink);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(template.DashboardLink))
            {
                sb.AppendLine("Go to your dashboard:");
                sb.AppendLine(template.DashboardLink);
                sb.AppendLine();
            }

            sb.AppendLine($"If you have any questions, feel free to contact our support team at {_emailConfig.Templates.Brand.SupportEmail}.");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        private string GenerateTicketConfirmationEmailText(TicketConfirmationEmailTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🎫 Your Ticket Confirmation");
            sb.AppendLine();
            sb.AppendLine($"Hi {template.To.Name},");
            sb.AppendLine();
            sb.AppendLine($"Great news! Your ticket for {template.Event.Title} has been confirmed.");
            sb.AppendLine();
            sb.AppendLine("Event Details:");
            sb.AppendLine($"Event: {template.Event.Title}");
            sb.AppendLine($"Date & Time: {template.Event.EventDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}");
            sb.AppendLine($"Venue: {template.Venue?.Name ?? "TBD"}");
            if (!string.IsNullOrEmpty(template.Venue?.Location))
            {
                sb.AppendLine($"Address: {template.Venue.Location}");
            }
            sb.AppendLine($"Ticket Type: {template.Ticket.EventPrice.Category}");
            sb.AppendLine($"Quantity: {template.Ticket.Quantity}");
            sb.AppendLine($"Total Amount: ${template.Ticket.TotalAmount:F2}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(template.TicketDownloadLink))
            {
                sb.AppendLine("Download your ticket:");
                sb.AppendLine(template.TicketDownloadLink);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(template.EventDetailsLink))
            {
                sb.AppendLine("View event details:");
                sb.AppendLine(template.EventDetailsLink);
                sb.AppendLine();
            }

            sb.AppendLine("Important Information:");
            sb.AppendLine("- Please arrive 30 minutes before the event starts");
            sb.AppendLine("- Bring a valid ID for verification");
            sb.AppendLine("- Screenshots of tickets are not accepted - please show the original QR code");
            sb.AppendLine();
            sb.AppendLine("We can't wait to see you at the event!");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        private string GeneratePaymentConfirmationEmailText(PaymentConfirmationEmailTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("💳 Payment Confirmation");
            sb.AppendLine();
            sb.AppendLine($"Hi {template.To.Name},");
            sb.AppendLine();
            sb.AppendLine("Your payment has been successfully processed! Here are the details:");
            sb.AppendLine();
            sb.AppendLine("Payment Details:");
            sb.AppendLine($"Transaction ID: {template.Payment.TransactionId}");
            sb.AppendLine($"Amount: ${template.Payment.Amount:F2}");
            sb.AppendLine($"Payment Method: {template.Payment.PaymentMethod}");
            sb.AppendLine($"Date: {template.Payment.PaymentDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}");
            sb.AppendLine($"Status: {template.Payment.Status}");
            sb.AppendLine();
            sb.AppendLine("Event Information:");
            sb.AppendLine($"Event: {template.Event.Title}");
            sb.AppendLine($"Date: {template.Event.EventDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}");
            sb.AppendLine($"Ticket Type: {template.Ticket.EventPrice.Category}");
            sb.AppendLine($"Quantity: {template.Ticket.Quantity}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(template.ReceiptDownloadLink))
            {
                sb.AppendLine("Download your receipt:");
                sb.AppendLine(template.ReceiptDownloadLink);
                sb.AppendLine();
            }

            sb.AppendLine("Your ticket confirmation will be sent separately. If you have any questions about your payment, please contact our support team.");
            sb.AppendLine();
            sb.AppendLine("Thank you for your purchase!");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        private string GenerateEventReminderEmailText(EventReminderEmailTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("⏰ Event Reminder");
            sb.AppendLine();
            sb.AppendLine($"Hi {template.To.Name},");
            sb.AppendLine();
            sb.AppendLine($"This is a friendly reminder that {template.Event.Title} is starting in {template.TimeUntilEvent}!");
            sb.AppendLine();
            sb.AppendLine("Event Details:");
            sb.AppendLine($"Event: {template.Event.Title}");
            sb.AppendLine($"Date & Time: {template.Event.EventDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}");
            sb.AppendLine($"Venue: {template.Venue?.Name ?? "TBD"}");
            if (!string.IsNullOrEmpty(template.Venue?.Location))
            {
                sb.AppendLine($"Address: {template.Venue.Location}");
            }
            sb.AppendLine();

            if (template.UserTickets.Any())
            {
                sb.AppendLine("Your Tickets:");
                foreach (var ticket in template.UserTickets)
                {
                    sb.AppendLine($"- {ticket.EventPrice.Category} - Quantity: {ticket.Quantity}");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(template.DirectionsLink))
            {
                sb.AppendLine("Get directions:");
                sb.AppendLine(template.DirectionsLink);
                sb.AppendLine();
            }

            sb.AppendLine("Reminders:");
            sb.AppendLine("- Arrive 30 minutes early");
            sb.AppendLine("- Bring your ticket QR code");
            sb.AppendLine("- Bring a valid ID");
            sb.AppendLine("- Check for any last-minute updates");
            sb.AppendLine();
            sb.AppendLine("We're excited to see you there!");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        private string GenerateEventCancellationEmailText(EventCancellationEmailTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("❌ Event Cancellation Notice");
            sb.AppendLine();
            sb.AppendLine($"Hi {template.To.Name},");
            sb.AppendLine();
            sb.AppendLine($"We regret to inform you that {template.Event.Title} scheduled for {template.Event.EventDate:dddd, MMMM dd, yyyy} has been cancelled.");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(template.CancellationReason))
            {
                sb.AppendLine("Reason for Cancellation:");
                sb.AppendLine(template.CancellationReason);
                sb.AppendLine();
            }

            if (template.AffectedTickets.Any())
            {
                sb.AppendLine("Your Affected Tickets:");
                foreach (var ticket in template.AffectedTickets)
                {
                    sb.AppendLine($"- {ticket.EventPrice.Category} - Quantity: {ticket.Quantity} - Amount: ${ticket.TotalAmount:F2}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Refund Information:");
            sb.AppendLine($"Refund Amount: ${template.RefundDetails.RefundAmount:F2}");
            sb.AppendLine($"Refund Method: {template.RefundDetails.RefundMethod}");
            sb.AppendLine($"Processing Time: {template.RefundDetails.ProcessingTime}");
            if (!string.IsNullOrEmpty(template.RefundDetails.ReferenceNumber))
            {
                sb.AppendLine($"Reference Number: {template.RefundDetails.ReferenceNumber}");
            }
            sb.AppendLine();

            sb.AppendLine("Your refund will be processed automatically. You should see the refund in your account within the specified processing time.");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(template.SupportContactInfo))
            {
                sb.AppendLine($"If you have any questions or concerns, please contact our support team at {template.SupportContactInfo}.");
                sb.AppendLine();
            }

            sb.AppendLine("We sincerely apologize for any inconvenience this may cause and appreciate your understanding.");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        private string GeneratePasswordResetEmailText(PasswordResetEmailTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🔒 Password Reset Request");
            sb.AppendLine();
            sb.AppendLine($"Hi {template.User.FullName ?? template.To.Name},");
            sb.AppendLine();
            sb.AppendLine("We received a request to reset your password. If you made this request, click the link below to reset your password:");
            sb.AppendLine();
            sb.AppendLine(template.ResetLink);
            sb.AppendLine();
            sb.AppendLine("Security Information:");
            sb.AppendLine($"- This link will expire on {template.LinkExpiresAt:dddd, MMMM dd, yyyy 'at' hh:mm tt}");
            sb.AppendLine($"- Request made from IP: {template.RequestIpAddress}");
            sb.AppendLine("- If you didn't request this, please ignore this email");
            sb.AppendLine();
            sb.AppendLine("If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.");
            sb.AppendLine();
            sb.AppendLine("For security reasons, this link will expire in 24 hours.");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        private string GenerateEmailVerificationEmailText(EmailVerificationEmailTemplate template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📧 Verify Your Email Address");
            sb.AppendLine();
            sb.AppendLine($"Hi {template.User.FullName ?? template.To.Name},");
            sb.AppendLine();
            sb.AppendLine($"Thank you for signing up with {_emailConfig.Templates.Brand.CompanyName}! To complete your registration, please verify your email address.");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(template.VerificationCode))
            {
                sb.AppendLine("Enter this 6-digit verification code:");
                sb.AppendLine($"*** {template.VerificationCode} ***");
                sb.AppendLine();
                sb.AppendLine("This code will expire in 15 minutes for security reasons.");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(template.VerificationLink))
            {
                sb.AppendLine("Or click the link below if you prefer:");
                sb.AppendLine(template.VerificationLink);
                sb.AppendLine();
            }

            sb.AppendLine("If you didn't create an account with us, you can safely ignore this email.");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        private string GenerateGenericEmailText<T>(T template) where T : EmailTemplateBase
        {
            var sb = new StringBuilder();
            sb.AppendLine(template.Subject);
            sb.AppendLine();
            sb.AppendLine($"Hi {template.To.Name},");
            sb.AppendLine();
            sb.AppendLine($"This is a notification from {_emailConfig.Templates.Brand.CompanyName}.");
            sb.AppendLine();
            sb.AppendLine($"Template Type: {template.TemplateType}");
            sb.AppendLine();
            sb.AppendLine($"If you have any questions, please contact our support team at {_emailConfig.Templates.Brand.SupportEmail}.");
            sb.AppendLine();
            sb.AppendLine("Best regards,");
            sb.AppendLine($"The {_emailConfig.Templates.Brand.CompanyName} Team");

            return sb.ToString();
        }

        #endregion

        #region Email Styling and Layout

        private string GetEmailStyles()
        {
            return $@"
<style>
    body {{
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        line-height: 1.6;
        color: #333;
        max-width: 600px;
        margin: 0 auto;
        padding: 0;
        background-color: #f8f9fa;
    }}
    .container {{
        background-color: #ffffff;
        margin: 20px auto;
        border-radius: 8px;
        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        overflow: hidden;
    }}
    .header {{
        background-color: {_emailConfig.Templates.Brand.PrimaryColor};
        color: white;
        padding: 20px;
        text-align: center;
    }}
    .header h1 {{
        margin: 0;
        font-size: 24px;
    }}
    .content {{
        padding: 30px;
    }}
    .content h1 {{
        color: {_emailConfig.Templates.Brand.PrimaryColor};
        margin-top: 0;
    }}
    .content h2 {{
        color: {_emailConfig.Templates.Brand.SecondaryColor};
        border-bottom: 2px solid #eee;
        padding-bottom: 10px;
    }}
    .content h3 {{
        color: {_emailConfig.Templates.Brand.SecondaryColor};
    }}
    .btn {{
        display: inline-block;
        padding: 12px 24px;
        text-decoration: none;
        border-radius: 5px;
        font-weight: bold;
        text-align: center;
        margin: 10px 5px;
    }}
    .btn-primary {{
        background-color: {_emailConfig.Templates.Brand.PrimaryColor};
        color: white;
    }}
    .btn-secondary {{
        background-color: {_emailConfig.Templates.Brand.SecondaryColor};
        color: white;
    }}
    .cta-section {{
        text-align: center;
        margin: 30px 0;
    }}
    table {{
        width: 100%;
        border-collapse: collapse;
        margin: 15px 0;
    }}
    table td {{
        padding: 8px;
        border-bottom: 1px solid #eee;
        vertical-align: top;
    }}
    table td:first-child {{
        font-weight: bold;
        width: 30%;
    }}
    .ticket-info, .payment-info, .event-info, .refund-info {{
        background-color: #f8f9fa;
        padding: 20px;
        border-radius: 5px;
        margin: 20px 0;
    }}
    .important-info {{
        background-color: #fff3cd;
        border: 1px solid #ffeaa7;
        padding: 15px;
        border-radius: 5px;
        margin: 20px 0;
    }}
    .security-info {{
        background-color: #f8d7da;
        border: 1px solid #f5c6cb;
        padding: 15px;
        border-radius: 5px;
        margin: 20px 0;
    }}
    .qr-section {{
        text-align: center;
        margin: 20px 0;
    }}
    .verification-code {{
        text-align: center;
        margin: 20px 0;
    }}
    .footer {{
        background-color: #f8f9fa;
        padding: 20px;
        text-align: center;
        font-size: 12px;
        color: {_emailConfig.Templates.Brand.SecondaryColor};
        border-top: 1px solid #dee2e6;
    }}
    .footer a {{
        color: {_emailConfig.Templates.Brand.PrimaryColor};
        text-decoration: none;
    }}
    ul {{
        padding-left: 20px;
    }}
    li {{
        margin-bottom: 5px;
    }}
</style>";
        }

        private string GetEmailHeader()
        {
            return $@"
<div class='header'>
    {(!string.IsNullOrEmpty(_emailConfig.Templates.Brand.LogoUrl) ?
        $"<img src='{_emailConfig.Templates.Brand.LogoUrl}' alt='{_emailConfig.Templates.Brand.CompanyName}' style='max-height: 50px; margin-bottom: 10px;' />" : "")}
    <h1>{_emailConfig.Templates.Brand.CompanyName}</h1>
</div>";
        }

        private string GetEmailFooter()
        {
            return $@"
<div class='footer'>
    <p>&copy; {DateTime.UtcNow.Year} {_emailConfig.Templates.Brand.CompanyName}. All rights reserved.</p>
    <p>
        <a href='{_emailConfig.Templates.Brand.WebsiteUrl}'>Visit our website</a> | 
        <a href='mailto:{_emailConfig.Templates.Brand.SupportEmail}'>Contact Support</a>
    </p>
    <p style='margin-top: 15px; font-size: 11px;'>
        This email was sent from {_emailConfig.Templates.Brand.CompanyName}. 
        If you have any questions, please contact us at {_emailConfig.Templates.Brand.SupportEmail}.
    </p>
</div>";
        }

        #endregion

        #region Helper Methods

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}