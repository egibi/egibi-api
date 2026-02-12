namespace egibi_api.Services.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email verification link after a new access request is submitted.
        /// </summary>
        Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationLink);

        /// <summary>
        /// Notifies the user that their access request has been approved and they can log in.
        /// </summary>
        Task SendAccessApprovedAsync(string toEmail, string firstName, string loginUrl);

        /// <summary>
        /// Notifies the user that their access request has been denied.
        /// </summary>
        Task SendAccessDeniedAsync(string toEmail, string firstName, string? reason);

        /// <summary>
        /// Sends a password reset link.
        /// </summary>
        Task SendPasswordResetAsync(string toEmail, string resetLink);
    }
}
