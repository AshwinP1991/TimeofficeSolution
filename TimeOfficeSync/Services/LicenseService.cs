using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace TimeOfficeSync.Services;

public class LicenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseService> _logger;
    private readonly EmailService _emailService;
    private readonly string _connectionString;

    private static readonly byte[] Key = Encoding.UTF8.GetBytes("TimeOffice2026K!");
    private bool _emailSent = false;

    public LicenseService(IConfiguration configuration, ILogger<LicenseService> logger, EmailService emailService)
    {
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _connectionString = configuration["DatabaseSettings:ConnectionString"] ?? "";
    }

    public async Task<bool> IsLicenseValidAsync()
    {
        try
        {
            var encryptedKey = _configuration["LicenseSettings:LicenseKey"];
            if (string.IsNullOrEmpty(encryptedKey))
            {
                LogLicense("No license key found", false, null, "License key missing");
                _logger.LogWarning("No license key found. License invalid.");
                return false;
            }

            var expiryDate = Decrypt(encryptedKey);
            var remainingDays = (expiryDate - DateTime.Now).Days;

            if (DateTime.Now > expiryDate)
            {
                var daysExpired = (DateTime.Now - expiryDate).Days;
                LogLicense("License expired", true, expiryDate, $"Expired on {expiryDate:dd/MM/yyyy}");
                _logger.LogWarning("License expired on {ExpiryDate}", expiryDate);

                if (!_emailSent)
                {
                    await _emailService.SendLicenseExpiredEmailAsync(expiryDate, daysExpired);
                    _emailSent = true;
                }
                return false;
            }

            _emailSent = false;
            LogLicense("License valid", false, expiryDate, $"{remainingDays} days remaining");
            _logger.LogInformation("License expiry date: {ExpiryDate}. {Days} days remaining.", expiryDate, remainingDays);
            return true;
        }
        catch (Exception ex)
        {
            LogLicense("License validation failed", false, null, ex.Message);
            _logger.LogError(ex, "License validation failed");
            return false;
        }
    }

    public DateTime GetExpiryDate()
    {
        var encryptedKey = _configuration["LicenseSettings:LicenseKey"];
        if (string.IsNullOrEmpty(encryptedKey))
            return DateTime.MinValue;
        return Decrypt(encryptedKey);
    }

    private void LogLicense(string message, bool isExpired, DateTime? expiryDate, string? details)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var sql = @"INSERT INTO [ApiLog] ([RequestUrl], [RequestTime], [Status], [RecordsCount], [ExceptionMsg])
                        VALUES (@RequestUrl, @RequestTime, @Status, @RecordsCount, @ExceptionMsg)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RequestUrl", $"LICENSE_CHECK: {message}");
            command.Parameters.AddWithValue("@RequestTime", DateTime.Now);
            command.Parameters.AddWithValue("@Status", isExpired ? "Expired" : "OK");
            command.Parameters.AddWithValue("@RecordsCount", 0);
            command.Parameters.AddWithValue("@ExceptionMsg", details ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log license check");
        }
    }

    public static string Encrypt(DateTime dateTime)
    {
        var plainText = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = new byte[16];

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public static DateTime Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = new byte[16];

        var buffer = Convert.FromBase64String(cipherText);
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        var plainText = sr.ReadToEnd();
        return DateTime.ParseExact(plainText, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
    }
}
