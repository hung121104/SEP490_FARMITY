export class RegisterOtpTemplate {
  static getSubject(): string {
    return 'Farmity — Verify Your Email';
  }

  static getText(username: string, otp: string): string {
    return `Hello ${username},

Welcome to Farmity! Your email verification code is: ${otp}

This code will expire in 10 minutes and can only be used once.

If you did not sign up for a Farmity account, please ignore this email.`;
  }

  static getHtml(username: string, otp: string): string {
    return `
      <div style="font-family: Arial, sans-serif; line-height:1.5; max-width: 600px; margin: 0 auto;">
        <div style="background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%); padding: 20px; text-align: center; border-radius: 8px 8px 0 0;">
          <h1 style="color: white; margin: 0;">Welcome to FARMITY!</h1>
        </div>
        <div style="background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px;">
          <p>Hello <strong>${username}</strong>,</p>
          <p>Thank you for signing up! Please enter the following verification code to complete your registration:</p>
          <div style="background: white; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px; border: 2px solid #43e97b;">
            <div style="font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #38b2ac; font-family: 'Courier New', monospace;">${otp}</div>
          </div>
          <p><strong>⏱️ Expires in: 10 minutes</strong></p>
          <p style="color: #666; font-size: 14px;">This code can be used only once.</p>
          <hr style="border: none; border-top: 1px solid #ddd; margin: 20px 0;">
          <p style="color: #999; font-size: 12px;">If you did not create an account on Farmity, please ignore this email.</p>
        </div>
      </div>
    `;
  }
}
