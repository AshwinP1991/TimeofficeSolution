# License Generator - SOFTO ALERT

Tool to generate encrypted license keys and credentials for TimeOfficeSync.

## Location

```
D:\Projects\timeoffice\LicenseGen\
```

## How to Run

Open PowerShell and navigate to the folder:

```powershell
cd D:\Projects\timeoffice\LicenseGen
```

## Commands

### 1. Generate License Key (for expiry date)

```powershell
dotnet run -- license "2026-12-31 23:59:59"
```

**Output:** Encrypted license key to paste in `appsettings.json` → `LicenseSettings:LicenseKey`

### 2. Encrypt Any Text (for email credentials)

```powershell
dotnet run -- encrypt "info@softovista.com"
dotnet run -- encrypt "your-password"
```

**Output:** Encrypted text to paste in `appsettings.json` → `EmailSettings:EncryptedUsername` or `EncryptedPassword`

### 3. Decrypt Text

```powershell
dotnet run -- decrypt "encrypted-text-here"
```

**Output:** Original plain text

## Examples

### Generate License Key

```powershell
# For 1 month expiry
dotnet run -- license "2026-08-31 23:59:59"

# For 6 months expiry
dotnet run -- license "2027-01-31 23:59:59"

# For 1 year expiry
dotnet run -- license "2027-07-31 23:59:59"
```

### Encrypt Email Credentials

```powershell
# Encrypt username
dotnet run -- encrypt "info@softovista.com"
# Output: bSnCUTtySLYVZd6TeWVnriMjbu1UgP5jZAgAwW9xYOU=

# Encrypt password
dotnet run -- encrypt "qczsphsyqrhnsntl"
# Output: pV8nPwfVkTlsfhr1InM6aGCnQ3828DISB46+guqlFc0=
```

## Update appsettings.json

After generating keys, update `appsettings.json`:

```json
{
  "LicenseSettings": {
    "LicenseKey": "paste-generated-license-key-here"
  },
  "EmailSettings": {
    "EncryptedUsername": "paste-encrypted-username-here",
    "EncryptedPassword": "paste-encrypted-password-here"
  }
}
```

## Build Solution

```powershell
cd D:\Projects\timeoffice
dotnet build
```

## Notes

- All encryption uses AES with key: `TimeOffice2026K!`
- License key format: `yyyy-MM-dd HH:mm:ss`
- Encrypted values are Base64 encoded
