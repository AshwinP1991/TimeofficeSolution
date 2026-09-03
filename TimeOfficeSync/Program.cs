using TimeOfficeSync;
using TimeOfficeSync.Services;

// --- Decrypt / Encrypt utility mode (does not start the worker) ---
// Usage:
//   dotnet run -- --decrypt "<cipher>" [--key "<key>"]
//   dotnet run -- --decrypt-license "<licenseKey>" [--key "<key>"]
//   dotnet run -- --encrypt "<plainText>" [--key "<key>"]
//   dotnet run -- --show-license   (decrypts LicenseSettings:LicenseKey from appsettings.json)
if (args.Length > 0 && (args[0].Equals("--decrypt", StringComparison.OrdinalIgnoreCase)
    || args[0].Equals("--decrypt-license", StringComparison.OrdinalIgnoreCase)
    || args[0].Equals("--encrypt", StringComparison.OrdinalIgnoreCase)
    || args[0].Equals("--show-license", StringComparison.OrdinalIgnoreCase)))
{
    var mode = args[0].ToLower();
    string? value = null;
    string? keyFlag = null;
    for (int i = 1; i < args.Length; i++)
    {
        if ((args[i] == "--key" || args[i] == "-k") && i + 1 < args.Length) { keyFlag = args[++i]; }
        else if (value == null) { value = args[i]; }
    }

    var tempConfig = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .Build();
    string passphrase;
    if (!string.IsNullOrWhiteSpace(keyFlag))
    {
        passphrase = keyFlag.Trim();
    }
    else
    {
        try
        {
            passphrase = CryptoHelper.GetPassphrase(tempConfig);
        }
        catch
        {
            // No key in config -> ask as user input (no default)
            Console.Write("Enter encryption key: ");
            passphrase = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                Console.WriteLine("Encryption key is required.");
                return;
            }
        }
    }

    try
    {
        if (mode == "--show-license")
        {
            value = tempConfig["LicenseSettings:LicenseKey"];
            if (string.IsNullOrWhiteSpace(value)) { Console.WriteLine("No LicenseSettings:LicenseKey found."); return; }
            mode = "--decrypt-license";
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            Console.Write(mode.StartsWith("--decrypt") ? "Enter text to decrypt: " : "Enter text to encrypt: ");
            value = Console.ReadLine()?.Trim() ?? "";
        }
        if (mode == "--encrypt")
        {
            Console.WriteLine(CryptoHelper.Encrypt(value, passphrase));
        }
        else if (mode == "--decrypt-license")
        {
            var plain = CryptoHelper.DecryptToString(value.Trim(), passphrase);
            var expiry = DateTime.ParseExact(plain, "yyyy-MM-dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
            var remaining = (expiry - DateTime.Now).Days;
            Console.WriteLine($"Expiry : {expiry:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine(remaining >= 0
                ? $"Status : VALID ({remaining} days remaining)"
                : $"Status : EXPIRED ({-remaining} days ago)");
        }
        else
        {
            Console.WriteLine(CryptoHelper.DecryptToString(value.Trim(), passphrase));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed: {ex.Message}");
    }
    return;
}

var builder = Host.CreateApplicationBuilder(args);

// Configure as Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SoftoLogsync";
});

// Register services
builder.Services.AddHttpClient<ApiService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<LicenseService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
