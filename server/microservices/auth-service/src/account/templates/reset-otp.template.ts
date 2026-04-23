export class ResetOtpTemplate {
  static getSubject(): string {
    return 'The Peaceful Farmstead - Password Reset OTP';
  }

  static getText(username: string, otp: string): string {
    return `Hello ${username},

We received a request to reset your password for The Peaceful Farmstead.

Your one-time password (OTP) is: ${otp}

This OTP will expire in 2 minutes and can only be used once.

If you did not request this password reset, please ignore this email.`;
  }

  static getHtml(username: string, otp: string): string {
    return `
      <div style="font-family: Arial, sans-serif; line-height:1.5; max-width: 600px; margin: 0 auto;">
        <div style="background: linear-gradient(135deg, #2f855a 0%, #68d391 100%); padding: 20px; text-align: center; border-radius: 8px 8px 0 0;">
          <h1 style="color: white; margin: 0;">The Peaceful Farmstead</h1>
        </div>
        <div style="background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px;">
          <p>Hello <strong>${username}</strong>,</p>
          <p>We received a request to reset your account password.</p>
          <p>Your one-time password (OTP) is:</p>
          <div style="background: white; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px; border: 2px solid #2f855a;">
            <div style="font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #2f855a; font-family: 'Courier New', monospace;">${otp}</div>
          </div>
          <p><strong>Expires in: 2 minutes</strong></p>
          <p style="color: #666; font-size: 14px;">This OTP can be used only once and is valid for 2 minutes.</p>
          <hr style="border: none; border-top: 1px solid #ddd; margin: 20px 0;">
          <p style="color: #999; font-size: 12px;">If you did not request this password reset, please ignore this email. For account safety, do not share this OTP with anyone.</p>
        </div>
      </div>
    `;
  }
}