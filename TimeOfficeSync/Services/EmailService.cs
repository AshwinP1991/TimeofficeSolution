using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace TimeOfficeSync.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("TimeOffice2026K!");

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendLicenseExpiredEmailAsync(DateTime expiryDate, int daysExpired)
    {
        var enabled = _configuration.GetValue<bool>("EmailSettings:Enabled", false);
        if (!enabled)
        {
            _logger.LogInformation("Email notifications disabled");
            return;
        }

        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "";
            var port = _configuration.GetValue<int>("EmailSettings:Port", 587);
            var enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl", true);
            var username = Decrypt(_configuration["EmailSettings:EncryptedUsername"] ?? "");
            var password = Decrypt(_configuration["EmailSettings:EncryptedPassword"] ?? "");
            var from = _configuration["EmailSettings:From"] ?? "";
            var displayName = _configuration["EmailSettings:DisplayName"] ?? "";
            var to = _configuration["EmailSettings:To"] ?? "";
            var cc = _configuration["EmailSettings:CC"] ?? "";

            var clientName = _configuration["EmailSettings:ClientName"] ?? "";
            var subject = $"[SOFTO ALERT] License Expired - {clientName}";
            var body = $@"
                <html>
                <body>
                    <h2>SOFTO ALERT - License Expired</h2>
                    <p>The license for TimeOffice Sync Service has expired.</p>
                    <table border='1' cellpadding='5' cellspacing='0'>
                        <tr><td><b>Client Name</b></td><td>{clientName}</td></tr>
                        <tr><td><b>Expiry Date</b></td><td>{expiryDate:dd/MM/yyyy HH:mm:ss}</td></tr>
                        <tr><td><b>Days Expired</b></td><td>{daysExpired} days</td></tr>
                        <tr><td><b>Server</b></td><td>{Environment.MachineName}</td></tr>
                        <tr><td><b>Check Time</b></td><td>{DateTime.Now:dd/MM/yyyy HH:mm:ss}</td></tr>
                    </table>
                    <p>Please renew the license to continue sync.</p>
                </body>
                </html>";

            using var message = new MailMessage();
            message.From = new MailAddress(from, displayName);
            message.To.Add(to);
            if (!string.IsNullOrEmpty(cc))
            {
                foreach (var ccEmail in cc.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    message.CC.Add(ccEmail.Trim());
                }
            }
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("License expired email sent to {To}, CC: {CC}", to, cc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send license expired email");
        }
    }

    private static string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = new byte[16];

        var buffer = Convert.FromBase64String(cipherText);
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
