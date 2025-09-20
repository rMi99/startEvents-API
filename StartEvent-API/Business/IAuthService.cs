using StartEvent_API.Data.Entities;
using StartEvent_API.Models.Auth;

namespace StartEvent_API.Business
{
    /// <summary>
    /// Service interface for handling user authentication, registration, and user management operations.
    /// Provides comprehensive user lifecycle management including registration, login, password reset,
    /// email verification, and user profile management.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system with the specified credentials.
        /// Creates a new user account with email verification requirements.
        /// </summary>
        /// <param name="user">The user entity containing registration details (email, full name, etc.)</param>
        /// <param name="password">The plain text password to be hashed and stored</param>
        /// <returns>The created ApplicationUser if successful, null if registration fails</returns>
        /// <exception cref="ArgumentException">Thrown when user data is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when user already exists</exception>
        Task<ApplicationUser?> RegisterAsync(ApplicationUser user, string password);

        /// <summary>
        /// Authenticates a user with email and password credentials.
        /// Validates login credentials and updates last login timestamp.
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <param name="password">The user's plain text password</param>
        /// <returns>The authenticated ApplicationUser if successful, null if authentication fails</returns>
        /// <exception cref="ArgumentException">Thrown when email or password is invalid</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when credentials are incorrect</exception>
        Task<ApplicationUser?> LoginAsync(string email, string password);

        /// <summary>
        /// Logs out a user by invalidating their authentication session.
        /// Updates security stamps and clears session-related data.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to log out</param>
        /// <returns>True if logout was successful, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when userId is invalid</exception>
        Task<bool> LogoutAsync(string userId);

        /// <summary>
        /// Creates a new admin user with elevated privileges.
        /// Only accessible by existing admin users for user management purposes.
        /// </summary>
        /// <param name="user">The user entity with admin details</param>
        /// <param name="password">The plain text password for the admin account</param>
        /// <returns>The created admin ApplicationUser if successful, null if creation fails</returns>
        /// <exception cref="ArgumentException">Thrown when user data is invalid</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when caller lacks admin privileges</exception>
        Task<ApplicationUser?> CreateAdminUserAsync(ApplicationUser user, string password);

        /// <summary>
        /// Retrieves all users with admin role assignments.
        /// Used for administrative user management and oversight.
        /// </summary>
        /// <returns>A list of all ApplicationUsers with admin privileges</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when caller lacks admin privileges</exception>
        Task<List<ApplicationUser>> GetAllAdminUsersAsync();

        /// <summary>
        /// Sends an email verification code to the specified user.
        /// Generates a verification token and sends it via email for account activation.
        /// </summary>
        /// <param name="request">The email verification request containing user email and verification details</param>
        /// <returns>EmailVerificationResponse indicating success/failure and any relevant messages</returns>
        /// <exception cref="ArgumentException">Thrown when email is invalid or user not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when email is already verified</exception>
        Task<EmailVerificationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request);

        /// <summary>
        /// Verifies a user's email address using the provided verification code.
        /// Completes the email verification process and activates the user account.
        /// </summary>
        /// <param name="request">The verification request containing email and verification code</param>
        /// <returns>VerifyEmailResponse indicating verification success/failure</returns>
        /// <exception cref="ArgumentException">Thrown when verification code is invalid or expired</exception>
        /// <exception cref="InvalidOperationException">Thrown when email is already verified</exception>
        Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request);

        /// <summary>
        /// Resends the email verification code to a user.
        /// Generates a new verification token and sends it via email if previous attempts failed.
        /// </summary>
        /// <param name="request">The resend request containing user email</param>
        /// <returns>ResendVerificationResponse indicating success/failure of resend operation</returns>
        /// <exception cref="ArgumentException">Thrown when email is invalid or user not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when email is already verified</exception>
        Task<ResendVerificationResponse> ResendEmailVerificationAsync(ResendEmailVerificationRequest request);

        // New user profile and management methods
        /// <summary>
        /// Retrieves the complete profile information for a specific user.
        /// Returns user details including personal information, organization data, and account status.
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>UserProfileResponse containing user profile data, null if user not found</returns>
        /// <exception cref="ArgumentException">Thrown when userId is invalid</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when caller lacks permission to view profile</exception>
        Task<UserProfileResponse?> GetUserProfileAsync(string userId);

        /// <summary>
        /// Updates a user's full name/display name.
        /// Allows users to modify their display name while maintaining email as the primary identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="newFullName">The new full name to be updated</param>
        /// <returns>True if update was successful, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when userId or newFullName is invalid</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when caller lacks permission to update profile</exception>
        Task<bool> UpdateUserNameAsync(string userId, string newFullName);

        /// <summary>
        /// Initiates the password reset process by sending a reset token to the user's email.
        /// Generates a secure reset token and emails it to the user for password recovery.
        /// </summary>
        /// <param name="email">The email address of the user requesting password reset</param>
        /// <returns>ResetPasswordResponse indicating success/failure and next steps</returns>
        /// <exception cref="ArgumentException">Thrown when email is invalid or user not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when account is locked or inactive</exception>
        Task<ResetPasswordResponse> InitiatePasswordResetAsync(string email);

        /// <summary>
        /// Confirms and completes the password reset using the provided reset token.
        /// Validates the reset token and updates the user's password if valid.
        /// </summary>
        /// <param name="email">The email address of the user</param>
        /// <param name="resetToken">The password reset token received via email</param>
        /// <param name="newPassword">The new password to be set</param>
        /// <returns>True if password reset was successful, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or token is expired</exception>
        /// <exception cref="SecurityTokenValidationException">Thrown when reset token is invalid</exception>
        Task<bool> ConfirmPasswordResetAsync(string email, string resetToken, string newPassword);

        // OTP-based Password Reset Methods
        /// <summary>
        /// Sends a One-Time Password (OTP) to the user's email for password reset.
        /// Alternative password reset method using OTP instead of token-based reset.
        /// </summary>
        /// <param name="email">The email address of the user requesting password reset</param>
        /// <returns>ForgotPasswordResponse indicating OTP send status and expiry information</returns>
        /// <exception cref="ArgumentException">Thrown when email is invalid or user not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when too many reset attempts have been made</exception>
        Task<ForgotPasswordResponse> SendPasswordResetOtpAsync(string email);

        /// <summary>
        /// Verifies the OTP provided by the user for password reset.
        /// Validates the OTP and returns a verification token for the final password reset step.
        /// </summary>
        /// <param name="email">The email address of the user</param>
        /// <param name="otp">The One-Time Password received via email</param>
        /// <returns>VerifyPasswordResetOtpResponse with verification status and reset token if successful</returns>
        /// <exception cref="ArgumentException">Thrown when OTP is invalid, expired, or email not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when max verification attempts exceeded</exception>
        Task<VerifyPasswordResetOtpResponse> VerifyPasswordResetOtpAsync(string email, string otp);

        /// <summary>
        /// Completes the OTP-based password reset process.
        /// Final step that updates the user's password after successful OTP verification.
        /// </summary>
        /// <param name="email">The email address of the user</param>
        /// <param name="resetToken">The reset token obtained from OTP verification</param>
        /// <param name="newPassword">The new password to be set</param>
        /// <returns>True if password reset was completed successfully, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or reset token is expired</exception>
        /// <exception cref="SecurityTokenValidationException">Thrown when reset token is invalid or tampered</exception>
        Task<bool> ResetPasswordWithOtpAsync(string email, string resetToken, string newPassword);
    }
}

