using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using StartEvent_API.Models.Email;
using StartEvent_API.Services.Email;
using System.Diagnostics;
using System.Text;

namespace StartEvent_API.Services.Email.Implementations
{
    /// <summary>
    /// Brevo (Sendinblue) email service implementation
    /// </summary>
    public class BrevoEmailService : IEmailService
    {
        private readonly EmailConfiguration _emailConfig;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<BrevoEmailService> _logger;
        private readonly RestClient _restClient;

        public BrevoEmailService(
            IOptions<EmailConfiguration> emailConfig,
            IEmailTemplateService templateService,
            ILogger<BrevoEmailService> logger)
        {
            _emailConfig = emailConfig.Value;
            _templateService = templateService;
            _logger = logger;

            var options = new RestClientOptions(_emailConfig.Brevo.ApiUrl)
            {
                Timeout = TimeSpan.FromSeconds(_emailConfig.Brevo.TimeoutSeconds)
            };
            _restClient = new RestClient(options);
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendEmailAsync<T>(T template) where T : EmailTemplateBase
        {
            return template switch
            {
                WelcomeEmailTemplate welcome => await SendWelcomeEmailAsync(welcome),
                TicketConfirmationEmailTemplate ticket => await SendTicketConfirmationEmailAsync(ticket),
                PaymentConfirmationEmailTemplate payment => await SendPaymentConfirmationEmailAsync(payment),
                EventReminderEmailTemplate reminder => await SendEventReminderEmailAsync(reminder),
                EventCancellationEmailTemplate cancellation => await SendEventCancellationEmailAsync(cancellation),
                PasswordResetEmailTemplate passwordReset => await SendPasswordResetEmailAsync(passwordReset),
                EmailVerificationEmailTemplate verification => await SendEmailVerificationEmailAsync(verification),
                _ => await SendGenericEmailAsync(template)
            };
        }

        /// <inheritdoc/>
        public async Task<BulkEmailSendResult> SendBulkEmailsAsync<T>(List<T> templates) where T : EmailTemplateBase
        {
            var result = new BulkEmailSendResult
            {
                TotalEmails = templates.Count
            };

            var successCount = 0;
            var failureCount = 0;

            var tasks = templates.Select(async template =>
            {
                var sendResult = await SendEmailAsync(template);
                result.Results.Add(sendResult);

                if (sendResult.Success)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);

                return sendResult;
            });

            await Task.WhenAll(tasks);

            result.SuccessfulEmails = successCount;
            result.FailedEmails = failureCount;

            await Task.WhenAll(tasks);

            _logger.LogInformation("Bulk email send completed. Total: {Total}, Success: {Success}, Failed: {Failed}",
                result.TotalEmails, result.SuccessfulEmails, result.FailedEmails);

            return result;
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendWelcomeEmailAsync(WelcomeEmailTemplate template)
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendTicketConfirmationEmailAsync(TicketConfirmationEmailTemplate template)
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send ticket confirmation email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendPaymentConfirmationEmailAsync(PaymentConfirmationEmailTemplate template)
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment confirmation email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendEventReminderEmailAsync(EventReminderEmailTemplate template)
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event reminder email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendEventCancellationEmailAsync(EventCancellationEmailTemplate template)
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event cancellation email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendPasswordResetEmailAsync(PasswordResetEmailTemplate template)
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendEmailVerificationEmailAsync(EmailVerificationEmailTemplate template)
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email verification email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateEmailServiceAsync()
        {
            try
            {
                var health = await GetServiceHealthAsync();
                return health.IsHealthy;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<EmailServiceHealth> GetServiceHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var health = new EmailServiceHealth();

            try
            {
                // Test Brevo API connectivity with account info endpoint
                var request = new RestRequest("/account", Method.Get);
                request.AddHeader("accept", "application/json");
                request.AddHeader("api-key", _emailConfig.Brevo.ApiKey);

                var response = await _restClient.ExecuteAsync(request);
                stopwatch.Stop();

                health.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

                if (response.IsSuccessful)
                {
                    health.IsHealthy = true;
                    health.Status = "Healthy - Brevo API accessible";
                }
                else
                {
                    health.IsHealthy = false;
                    health.Status = $"Unhealthy - API returned {response.StatusCode}";
                    health.ErrorDetails = response.Content;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                health.IsHealthy = false;
                health.Status = "Unhealthy - Exception occurred";
                health.ErrorDetails = ex.Message;
                health.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return health;
        }

        /// <summary>
        /// Sends a generic email for template types not specifically handled
        /// </summary>
        private async Task<EmailSendResult> SendGenericEmailAsync<T>(T template) where T : EmailTemplateBase
        {
            try
            {
                var renderedTemplate = await _templateService.RenderTemplateAsync(template);
                return await SendEmailViaBrevoAsync(template, renderedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send generic email to {Email}", template.To.Email);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RecipientEmail = template.To.Email,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Sends email via Brevo API with retry logic
        /// </summary>
        private async Task<EmailSendResult> SendEmailViaBrevoAsync<T>(T template, EmailTemplateResult renderedTemplate)
            where T : EmailTemplateBase
        {
            var result = new EmailSendResult
            {
                RecipientEmail = template.To.Email,
                Subject = template.Subject,
                TemplateType = template.TemplateType,
                SentAt = DateTime.UtcNow
            };

            if (!_emailConfig.General.EnableEmailSending)
            {
                _logger.LogInformation("Email sending is disabled. Would send email to {Email} with subject: {Subject}",
                    template.To.Email, template.Subject);
                result.Success = true;
                result.MessageId = "EMAIL_SENDING_DISABLED";
                return result;
            }

            if (_emailConfig.General.UseSandboxMode)
            {
                _logger.LogInformation("Sandbox mode: Email to {Email} with subject: {Subject}",
                    template.To.Email, template.Subject);
                result.Success = true;
                result.MessageId = "SANDBOX_MODE";
                return result;
            }

            var maxRetries = _emailConfig.General.MaxRetryAttempts;
            var retryDelay = TimeSpan.FromSeconds(_emailConfig.General.RetryDelaySeconds);

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    result.RetryAttempts = attempt;

                    var brevoRequest = CreateBrevoRequest(template, renderedTemplate);
                    var request = new RestRequest("/smtp/email", Method.Post);
                    request.AddHeader("accept", "application/json");
                    request.AddHeader("api-key", _emailConfig.Brevo.ApiKey);
                    request.AddHeader("content-type", "application/json");
                    request.AddJsonBody(brevoRequest);

                    if (_emailConfig.General.LogEmailContent)
                    {
                        _logger.LogDebug("Sending email to {Email}: {Content}",
                            template.To.Email, JsonConvert.SerializeObject(brevoRequest, Formatting.Indented));
                    }

                    var response = await _restClient.ExecuteAsync(request);

                    if (response.IsSuccessful && response.Content != null)
                    {
                        var brevoResponse = JsonConvert.DeserializeObject<BrevoSendEmailResponse>(response.Content);
                        _logger.LogInformation("Brevo response: {Response}",
                                                    JsonConvert.SerializeObject(brevoResponse, Formatting.Indented));
                        result.Success = true;
                        result.MessageId = brevoResponse?.MessageId;

                        _logger.LogInformation("Email sent successfully to {Email} with MessageId: {MessageId}",
                            template.To.Email, result.MessageId);

                        return result;
                    }
                    else
                    {
                        var errorMessage = $"Brevo API returned {response.StatusCode}: {response.Content}";

                        if (response.Content != null)
                        {
                            try
                            {
                                var errorResponse = JsonConvert.DeserializeObject<BrevoErrorResponse>(response.Content);
                                errorMessage = $"Brevo Error {errorResponse?.Code}: {errorResponse?.Message}";
                            }
                            catch
                            {
                                // Use the original error message if JSON parsing fails
                            }
                        }

                        if (attempt < maxRetries)
                        {
                            _logger.LogWarning("Email send attempt {Attempt} failed for {Email}: {Error}. Retrying in {Delay}s...",
                                attempt + 1, template.To.Email, errorMessage, retryDelay.TotalSeconds);
                            await Task.Delay(retryDelay);
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = errorMessage;
                            _logger.LogError("Failed to send email to {Email} after {Attempts} attempts: {Error}",
                                template.To.Email, maxRetries + 1, errorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning(ex, "Email send attempt {Attempt} failed for {Email}. Retrying in {Delay}s...",
                            attempt + 1, template.To.Email, retryDelay.TotalSeconds);
                        await Task.Delay(retryDelay);
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = ex.Message;
                        result.Exception = ex;
                        _logger.LogError(ex, "Failed to send email to {Email} after {Attempts} attempts",
                            template.To.Email, maxRetries + 1);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates Brevo API request from template
        /// </summary>
        private BrevoSendEmailRequest CreateBrevoRequest<T>(T template, EmailTemplateResult renderedTemplate)
            where T : EmailTemplateBase
        {
            var request = new BrevoSendEmailRequest
            {
                Sender = new BrevoEmailAddress
                {
                    Email = _emailConfig.Brevo.DefaultSender.Email,
                    Name = _emailConfig.Brevo.DefaultSender.Name
                },
                To = new List<BrevoEmailAddress>
                {
                    new()
                    {
                        Email = template.To.Email,
                        Name = template.To.Name
                    }
                },
                Subject = renderedTemplate.Subject,
                HtmlContent = renderedTemplate.HtmlContent,
                TextContent = renderedTemplate.TextContent,
                Tags = new List<string> { template.TemplateType.ToString(), "StartEvent" }
            };

            // Add attachments if any
            if (renderedTemplate.Attachments.Any())
            {
                request.Attachment = renderedTemplate.Attachments.Select(a => new BrevoAttachment
                {
                    Content = Convert.ToBase64String(a.Content),
                    Name = a.FileName
                }).ToList();
            }

            return request;
        }
    }
}