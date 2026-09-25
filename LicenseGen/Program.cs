using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

const string DefaultKey = "YOUR_SECRET_KEY_HERE";

app.MapPost("/api/encrypt", (EncryptRequest req) =>
{
    var key = GetKeyBytes(req.Key ?? DefaultKey);
    var encrypted = Encrypt(req.Text, key);
    return Results.Ok(new { encrypted });
});

app.MapPost("/api/decrypt", (DecryptRequest req) =>
{
    try
    {
        var key = GetKeyBytes(req.Key ?? DefaultKey);
        var decrypted = Decrypt(req.Text, key);
        return Results.Ok(new { decrypted });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/license/generate", (LicenseGenerateRequest req) =>
{
    var key = GetKeyBytes(req.Key ?? DefaultKey);
    var date = DateTime.Parse(req.Expiry);
    var license = Encrypt(date.ToString("yyyy-MM-dd HH:mm:ss"), key);
    return Results.Ok(new { license, expiry = date });
});

app.MapPost("/api/license/decrypt", (LicenseDecryptRequest req) =>
{
    try
    {
        var key = GetKeyBytes(req.Key ?? DefaultKey);
        var plain = Decrypt(req.License, key);
        var expiry = DateTime.ParseExact(plain, "yyyy-MM-dd HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture);
        var remaining = (expiry - DateTime.Now).Days;
        return Results.Ok(new
        {
            expiry,
            valid = remaining >= 0,
            remainingDays = remaining,
            status = remaining >= 0 ? "VALID" : "EXPIRED"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

app.Run();

static byte[] GetKeyBytes(string passphrase)
{
    var bytes = Encoding.UTF8.GetBytes(passphrase);
    if (bytes.Length == 16 || bytes.Length == 24 || bytes.Length == 32)
        return bytes;
    return SHA256.HashData(bytes);
}

static string Encrypt(string plainText, byte[] key)
{
    using var aes = Aes.Create();
    aes.Key = key;
    aes.IV = new byte[16];

    using var encryptor = aes.CreateEncryptor();
    using var ms = new MemoryStream();
    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
    using (var sw = new StreamWriter(cs)) { sw.Write(plainText); }
    return Convert.ToBase64String(ms.ToArray());
}

static string Decrypt(string cipherText, byte[] key)
{
    using var aes = Aes.Create();
    aes.Key = key;
    aes.IV = new byte[16];

    var buffer = Convert.FromBase64String(cipherText);
    using var decryptor = aes.CreateDecryptor();
    using var ms = new MemoryStream(buffer);
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using var sr = new StreamReader(cs);
    return sr.ReadToEnd();
}

record EncryptRequest(string Text, string? Key);
record DecryptRequest(string Text, string? Key);
record LicenseGenerateRequest(string Expiry, string? Key);
record LicenseDecryptRequest(string License, string? Key);
