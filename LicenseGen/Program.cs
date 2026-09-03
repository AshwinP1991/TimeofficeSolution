using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program
{

    static void Main(string[] args)
    {
        // No args -> interactive console mode: enter values, get encryption/decryption back
        if (args.Length == 0)
        {
            RunInteractive();
            return;
        }

        var command = args[0].ToLower();
        var positional = new List<string>();
        string? keyFromFlag = null;

        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "--key" || args[i] == "-k") && i + 1 < args.Length)
            {
                keyFromFlag = args[i + 1];
                i++;
            }
            else
            {
                positional.Add(args[i]);
            }
        }

        var value = positional.Count > 0 ? positional[0] : "";
        if (string.IsNullOrWhiteSpace(value))
            value = PromptForValue(command);

        // Key can come from: --key flag, 2nd positional arg, or interactive input field
        var keyInput = keyFromFlag
            ?? (positional.Count > 1 ? positional[1] : null)
            ?? PromptForKey();

        var key = GetKeyBytes(keyInput);

        switch (command)
        {
            case "encrypt":
                Console.WriteLine(Encrypt(value, key));
                break;
            case "decrypt":
                try { Console.WriteLine(Decrypt(value, key)); }
                catch (Exception ex) { Console.WriteLine($"Decrypt failed: {ex.Message}"); }
                break;
            case "license":
                var date = DateTime.Parse(value);
                Console.WriteLine(Encrypt(date.ToString("yyyy-MM-dd HH:mm:ss"), key));
                break;
            case "decrypt-license":
            case "decryptlicense":
            case "show-license":
            case "check-license":
                try
                {
                    var plain = Decrypt(value.Trim(), key);
                    var expiry = DateTime.ParseExact(plain, "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture);
                    var remaining = (expiry - DateTime.Now).Days;
                    Console.WriteLine($"Expiry : {expiry:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine(remaining >= 0
                        ? $"Status : VALID ({remaining} days remaining)"
                        : $"Status : EXPIRED ({-remaining} days ago)");
                }
                catch (Exception ex) { Console.WriteLine($"Decrypt-license failed: {ex.Message}"); }
                break;
            default:
                Console.WriteLine($"Unknown command '{command}'. Use encrypt|decrypt|license|decrypt-license");
                break;
        }
    }

    static void RunInteractive()
    {
        Console.WriteLine("=== LicenseGen Interactive ===");
        Console.WriteLine("Enter values, get encryption/decryption in return. Type 'exit' to quit.");
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Choose: 1=Encrypt  2=Decrypt  3=License(encrypt date)  4=Decrypt-License  5=Exit");
            Console.Write("Option [1-5]: ");
            var choice = Console.ReadLine()?.Trim().ToLower() ?? "";
            if (choice == "5" || choice == "exit" || choice == "quit" || choice == "q")
                break;

            string command = choice switch
            {
                "1" or "encrypt" => "encrypt",
                "2" or "decrypt" => "decrypt",
                "3" or "license" => "license",
                "4" or "decrypt-license" or "decryptlicense" or "show-license" or "check-license" => "decrypt-license",
                _ => ""
            };
            if (string.IsNullOrEmpty(command))
            {
                Console.WriteLine("Invalid option. Use 1-5.");
                continue;
            }

            var value = PromptForValue(command);
            if (value.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;
            var keyInput = PromptForKey();
            var key = GetKeyBytes(keyInput);

            try
            {
                switch (command)
                {
                    case "encrypt":
                        Console.WriteLine("Encrypted: " + Encrypt(value, key));
                        break;
                    case "decrypt":
                        Console.WriteLine("Decrypted: " + Decrypt(value, key));
                        break;
                    case "license":
                        var date = DateTime.Parse(value);
                        Console.WriteLine("LicenseKey: " + Encrypt(date.ToString("yyyy-MM-dd HH:mm:ss"), key));
                        break;
                    case "decrypt-license":
                        var plain = Decrypt(value.Trim(), key);
                        var expiry = DateTime.ParseExact(plain, "yyyy-MM-dd HH:mm:ss",
                            System.Globalization.CultureInfo.InvariantCulture);
                        var remaining = (expiry - DateTime.Now).Days;
                        Console.WriteLine($"Expiry : {expiry:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine(remaining >= 0
                            ? $"Status : VALID ({remaining} days remaining)"
                            : $"Status : EXPIRED ({-remaining} days ago)");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
            }
        }
        Console.WriteLine("Bye.");
    }

    static string PromptForValue(string command)
    {
        Console.Write(command.StartsWith("decrypt") ? "Enter text to decrypt: " : "Enter text to encrypt: ");
        return Console.ReadLine()?.Trim() ?? "";
    }

    static string PromptForKey()
    {
        while (true)
        {
            Console.Write("Enter encryption key: ");
            var input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(input))
                return input;
            Console.WriteLine("Key is required. Please enter a value.");
        }
    }

    static byte[] GetKeyBytes(string passphrase)
    {
        var bytes = Encoding.UTF8.GetBytes(passphrase);
        // Use directly if valid AES length (16/24/32), otherwise derive 256-bit key via SHA256
        // so any input-field value works.
        if (bytes.Length == 16 || bytes.Length == 24 || bytes.Length == 32)
            return bytes;
        return System.Security.Cryptography.SHA256.HashData(bytes);
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
}
