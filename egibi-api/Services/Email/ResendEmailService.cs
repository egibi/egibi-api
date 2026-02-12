using Resend;

namespace egibi_api.Services.Email
{
    public class ResendEmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService(
            IResend resend,
            IConfiguration config,
            ILogger<ResendEmailService> logger)
        {
            _resend = resend;
            _fromEmail = config["Resend:FromEmail"] ?? "noreply@egibi.io";
            _fromName = config["Resend:FromName"] ?? "Egibi";
            _logger = logger;
        }

        // =============================================
        // EMAIL VERIFICATION
        // =============================================

        public async Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationLink)
        {
            var greeting = string.IsNullOrWhiteSpace(firstName) ? "Hello" : $"Hello {firstName}";

            var html = $@"
                <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; padding: 40px 20px;"">
                    <h2 style=""color: #0f172a; margin-bottom: 8px;"">Verify your email</h2>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">{greeting},</p>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">
                        Thank you for requesting access to Egibi. Please verify your email address by clicking the button below.
                    </p>
                    <div style=""text-align: center; margin: 32px 0;"">
                        <a href=""{verificationLink}""
                           style=""display: inline-block; background: #0f172a; color: #ffffff; padding: 12px 32px;
                                  border-radius: 6px; text-decoration: none; font-weight: 500; font-size: 14px;"">
                            Verify Email Address
                        </a>
                    </div>
                    <p style=""color: #94a3b8; font-size: 13px; line-height: 1.6;"">
                        If you didn't request access to Egibi, you can safely ignore this email.
                        This link will expire in 24 hours.
                    </p>
                    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;"" />
                    <p style=""color: #94a3b8; font-size: 12px;"">Egibi — Multi-Asset Algorithmic Trading</p>
                </div>";

            await SendAsync(toEmail, "Verify your email — Egibi", html);
        }

        // =============================================
        // ACCESS APPROVED
        // =============================================

        public async Task SendAccessApprovedAsync(string toEmail, string firstName, string loginUrl)
        {
            var greeting = string.IsNullOrWhiteSpace(firstName) ? "Hello" : $"Hello {firstName}";

            var html = $@"
                <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; padding: 40px 20px;"">
                    <h2 style=""color: #0f172a; margin-bottom: 8px;"">You're in!</h2>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">{greeting},</p>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">
                        Your access request has been approved. You can now sign in to Egibi using the credentials you provided when you requested access.
                    </p>
                    <div style=""text-align: center; margin: 32px 0;"">
                        <a href=""{loginUrl}""
                           style=""display: inline-block; background: #0f172a; color: #ffffff; padding: 12px 32px;
                                  border-radius: 6px; text-decoration: none; font-weight: 500; font-size: 14px;"">
                            Sign In
                        </a>
                    </div>
                    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;"" />
                    <p style=""color: #94a3b8; font-size: 12px;"">Egibi — Multi-Asset Algorithmic Trading</p>
                </div>";

            await SendAsync(toEmail, "Your Egibi access has been approved", html);
        }

        // =============================================
        // ACCESS DENIED
        // =============================================

        public async Task SendAccessDeniedAsync(string toEmail, string firstName, string? reason)
        {
            var greeting = string.IsNullOrWhiteSpace(firstName) ? "Hello" : $"Hello {firstName}";

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"<p style=""color: #475569; font-size: 15px; line-height: 1.6;"">
                        <strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason)}
                     </p>";

            var html = $@"
                <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; padding: 40px 20px;"">
                    <h2 style=""color: #0f172a; margin-bottom: 8px;"">Access Request Update</h2>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">{greeting},</p>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">
                        We've reviewed your access request for Egibi and are unable to approve it at this time.
                    </p>
                    {reasonBlock}
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">
                        If you believe this was a mistake, please reach out to us.
                    </p>
                    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;"" />
                    <p style=""color: #94a3b8; font-size: 12px;"">Egibi — Multi-Asset Algorithmic Trading</p>
                </div>";

            await SendAsync(toEmail, "Your Egibi access request update", html);
        }

        // =============================================
        // PASSWORD RESET
        // =============================================

        public async Task SendPasswordResetAsync(string toEmail, string resetLink)
        {
            var html = $@"
                <div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; padding: 40px 20px;"">
                    <h2 style=""color: #0f172a; margin-bottom: 8px;"">Reset your password</h2>
                    <p style=""color: #475569; font-size: 15px; line-height: 1.6;"">
                        We received a request to reset your password. Click the button below to choose a new one.
                    </p>
                    <div style=""text-align: center; margin: 32px 0;"">
                        <a href=""{resetLink}""
                           style=""display: inline-block; background: #0f172a; color: #ffffff; padding: 12px 32px;
                                  border-radius: 6px; text-decoration: none; font-weight: 500; font-size: 14px;"">
                            Reset Password
                        </a>
                    </div>
                    <p style=""color: #94a3b8; font-size: 13px; line-height: 1.6;"">
                        If you didn't request a password reset, you can safely ignore this email.
                        This link will expire in 1 hour.
                    </p>
                    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;"" />
                    <p style=""color: #94a3b8; font-size: 12px;"">Egibi — Multi-Asset Algorithmic Trading</p>
                </div>";

            await SendAsync(toEmail, "Reset your password — Egibi", html);
        }

        // =============================================
        // INTERNAL SEND
        // =============================================

        private async Task SendAsync(string to, string subject, string html)
        {
            try
            {
                var message = new EmailMessage
                {
                    From = $"{_fromName} <{_fromEmail}>",
                    To = { to },
                    Subject = subject,
                    HtmlBody = html
                };

                await _resend.EmailSendAsync(message);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
                // Don't throw — email failures shouldn't break the main flow
            }
        }
    }
}
