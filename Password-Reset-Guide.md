# Password Reset API Guide

This guide provides step-by-step instructions on how to use the OTP-based password reset system when users forget their passwords.

## Overview

The password reset system consists of three simple steps:
1. **Request OTP** - Send user's email to receive an OTP
2. **Verify OTP** - Submit the OTP to get a reset token
3. **Reset Password** - Use the reset token to set a new password

## API Endpoints

### 1. Request Password Reset OTP

**Endpoint:** `POST /api/Auth/forgot-password`

**Purpose:** Initiates the password reset process by sending an OTP to the user's email.

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Success Response (200):**
```json
{
  "message": "An OTP has been sent to your email address. Please check your email and enter the OTP to continue with password reset.",
  "data": {
    "email": "user@example.com",
    "expiresAt": "2024-01-15T10:30:00Z",
    "remainingAttempts": 4,
    "sentAt": "2024-01-15T10:15:00Z"
  },
  "statusCode": 200
}
```

**Rate Limit Response (429):**
```json
{
  "message": "Too many password reset attempts. Please try again after 1 hour.",
  "statusCode": 429,
  "data": {
    "remainingAttempts": 0,
    "retryAfter": "1 hour"
  }
}
```

**Important Notes:**
- OTP expires in **15 minutes**
- Maximum **5 attempts per hour** per email
- OTP is a 6-digit numeric code
- For security, the API returns success even if the email doesn't exist

---

### 2. Verify Password Reset OTP

**Endpoint:** `POST /api/Auth/verify-reset-otp`

**Purpose:** Verifies the OTP received via email and returns a reset token for password change.

**Request Body:**
```json
{
  "email": "user@example.com",
  "otp": "123456"
}
```

**Success Response (200):**
```json
{
  "message": "OTP verified successfully. You can now reset your password.",
  "data": {
    "email": "user@example.com",
    "resetToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "verifiedAt": "2024-01-15T10:20:00Z",
    "tokenExpiresAt": "2024-01-15T10:50:00Z"
  },
  "statusCode": 200
}
```

**Error Response (400):**
```json
{
  "message": "OTP has expired or is invalid. Please request a new one.",
  "statusCode": 400
}
```

**Important Notes:**
- Reset token is valid for **30 minutes**
- OTP can only be used once
- After successful verification, the OTP is cleared

---

### 3. Reset Password with Token

**Endpoint:** `POST /api/Auth/reset-password-otp`

**Purpose:** Resets the user's password using the reset token obtained from OTP verification.

**Request Body:**
```json
{
  "email": "user@example.com",
  "resetToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "newPassword": "NewSecurePassword123!",
  "confirmPassword": "NewSecurePassword123!"
}
```

**Success Response (200):**
```json
{
  "message": "Password has been reset successfully. You can now log in with your new password.",
  "data": {
    "email": "user@example.com",
    "resetAt": "2024-01-15T10:25:00Z"
  },
  "statusCode": 200
}
```

**Error Response (400):**
```json
{
  "message": "Failed to reset password. The reset token may be invalid, expired, or the email may not exist.",
  "statusCode": 400
}
```

**Password Requirements:**
- Minimum 8 characters
- Must match confirmation password
- Should contain a mix of uppercase, lowercase, numbers, and special characters

---

## Complete Workflow Example

Here's a complete example of the password reset process:

### Step 1: Request OTP
```bash
curl -X POST "https://your-api-domain.com/api/Auth/forgot-password" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com"
  }'
```

### Step 2: Check Email and Verify OTP
```bash
curl -X POST "https://your-api-domain.com/api/Auth/verify-reset-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "otp": "567890"
  }'
```

### Step 3: Reset Password
```bash
curl -X POST "https://your-api-domain.com/api/Auth/reset-password-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "resetToken": "received-token-from-step-2",
    "newPassword": "MyNewSecurePassword123!",
    "confirmPassword": "MyNewSecurePassword123!"
  }'
```

---

## Security Features

### Rate Limiting
- **5 OTP requests per hour** per email address
- Automatic cooldown period of 1 hour after limit exceeded
- Counter resets after 1 hour from first attempt

### Time-based Security
- **OTP expires in 15 minutes** - Short window to prevent misuse
- **Reset token expires in 30 minutes** - Adequate time for password reset
- Automatic cleanup of expired tokens and OTPs

### Token Security
- OTP is immediately cleared after successful verification
- Reset token is unique and generated using GUID
- All tokens are cleared after successful password reset

### Email Security
- OTP is sent via secure email service
- No sensitive information stored in plain text
- Security-first approach: No revelation of email existence

---

## Error Handling

### Common Error Codes

| Status Code | Description | Action Required |
|-------------|-------------|-----------------|
| 400 | Bad Request - Invalid input or expired token | Check request format and token validity |
| 422 | Validation Failed - Input validation errors | Fix validation errors in request |
| 429 | Too Many Requests - Rate limit exceeded | Wait 1 hour before trying again |
| 500 | Internal Server Error - System error | Contact support or try again later |

### Troubleshooting

**OTP not received?**
- Check spam/junk folder
- Verify email address spelling
- Wait a few minutes (email delivery may be delayed)
- Try requesting a new OTP

**OTP expired?**
- Request a new OTP using the forgot-password endpoint
- OTPs are valid for only 15 minutes

**Reset token invalid?**
- Verify you're using the correct token from step 2
- Check if 30 minutes have passed since verification
- Complete the process within the time limit

---

## Integration Tips

### Frontend Implementation
1. **Step 1**: Show email input form, call forgot-password API
2. **Step 2**: Show OTP input form, call verify-reset-otp API
3. **Step 3**: Show password reset form, call reset-password-otp API
4. **Success**: Redirect to login page with success message

### Mobile App Implementation
- Consider deep linking from email OTP to app
- Implement automatic OTP detection from SMS/Email
- Show countdown timer for OTP expiry
- Provide "Resend OTP" functionality

### Error Handling Best Practices
- Always handle rate limiting gracefully
- Show user-friendly error messages
- Implement retry mechanisms for network failures
- Log errors for debugging but don't expose sensitive details

---

## Testing

You can test the complete flow using tools like Postman, curl, or any HTTP client following the examples above.

**Test Email:** Use a real email address you can access to receive the OTP.

**Test Password:** Use a strong password that meets the requirements.

---

*This documentation covers the complete OTP-based password reset system. For additional support or questions, please contact the development team.*