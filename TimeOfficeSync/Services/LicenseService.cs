using Microsoft.Data.SqlClient;

namespace TimeOfficeSync.Services;

public class LicenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseService> _logger;
    private readonly EmailService _emailService;
    private readonly string _connectionString;
    private readonly string _passphrase;
    private bool _emailSent = false;

    public LicenseService(IConfiguration configuration, ILogger<LicenseService> logger, EmailService emailService)
    {
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _connectionString = configuration["DatabaseSettings:ConnectionString"] ?? "";
        _passphrase = CryptoHelper.GetPassphrase(configuration);
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

            var expiryDate = DecryptLicense(encryptedKey);
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
        return DecryptLicense(encryptedKey);
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

    public static string Encrypt(DateTime dateTime, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase))
            throw new ArgumentException("Encryption key is required (user input).", nameof(passphrase));
        return CryptoHelper.Encrypt(dateTime.ToString("yyyy-MM-dd HH:mm:ss"), passphrase);
    }

    public DateTime DecryptLicense(string cipherText)
    {
        var plainText = CryptoHelper.DecryptToString(cipherText, _passphrase);
        return DateTime.ParseExact(plainText, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static DateTime Decrypt(string cipherText, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase))
            throw new ArgumentException("Encryption key is required (user input).", nameof(passphrase));
        var plainText = CryptoHelper.DecryptToString(cipherText, passphrase);
        return DateTime.ParseExact(plainText, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
    }
}
