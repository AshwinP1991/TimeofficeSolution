using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static readonly byte[] Key = Encoding.UTF8.GetBytes("TimeOffice2026K!");

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -- encrypt \"text to encrypt\"");
            Console.WriteLine("  dotnet run -- decrypt \"encrypted text\"");
            Console.WriteLine("  dotnet run -- license \"2026-08-31 23:59:59\"");
            return;
        }

        var command = args[0].ToLower();
        var value = args.Length > 1 ? args[1] : "";

        switch (command)
        {
            case "encrypt":
                Console.WriteLine(Encrypt(value));
                break;
            case "decrypt":
                Console.WriteLine(Decrypt(value));
                break;
            case "license":
                var date = DateTime.Parse(value);
                Console.WriteLine(Encrypt(date.ToString("yyyy-MM-dd HH:mm:ss")));
                break;
        }
    }

    static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = new byte[16];

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs)) { sw.Write(plainText); }
        return Convert.ToBase64String(ms.ToArray());
    }

    static string Decrypt(string cipherText)
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
