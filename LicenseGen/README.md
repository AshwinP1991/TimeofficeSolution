# License Generator API - SOFTO ALERT

ASP.NET Core Minimal API for generating encrypted license keys and credentials for TimeOfficeSync.

## API Endpoints

### Health Check
```http
GET /health
```

### Encrypt Text
```http
POST /api/encrypt
Content-Type: application/json

{
  "text": "info@softovista.com",
  "key": "optional-custom-key"
}
```

### Decrypt Text
```http
POST /api/decrypt
Content-Type: application/json

{
  "text": "encrypted-base64-string",
  "key": "optional-custom-key"
}
```

### Generate License Key
```http
POST /api/license/generate
Content-Type: application/json

{
  "expiry": "2026-12-31 23:59:59",
  "key": "optional-custom-key"
}
```

### Decrypt License
```http
POST /api/license/decrypt
Content-Type: application/json

{
  "license": "encrypted-license-key",
  "key": "optional-custom-key"
}
```

## IIS Hosting Setup

### Prerequisites
1. Install .NET 8 Hosting Bundle: https://dotnet.microsoft.com/download/dotnet/8.0
2. Enable ASP.NET Core Module in IIS

### Steps
```powershell
# Publish the app
cd D:\Projects\timeoffice\LicenseGen
dotnet publish -c Release -o C:\inetpub\LicenseGen

# Create IIS Site
# 1. Open IIS Manager
# 2. Right-click Sites > Add Website
# 3. Site name: LicenseGen
# 4. Physical path: C:\inetpub\LicenseGen
# 5. Port: 8080 (or your preferred port)
# 6. Click OK
```

### Verify Installation
```powershell
# Test health endpoint
curl http://localhost:8080/health

# Test encryption
curl -X POST http://localhost:8080/api/encrypt -H "Content-Type: application/json" -d "{\"text\": \"test\"}"
```

## Local Development

```powershell
# Run locally
dotnet run

# API will be available at http://localhost:5000
```

## Configuration

### Custom Encryption Key
The API uses a default key if none is provided. For production, set a custom key:

```json
{
  "text": "your-text",
  "key": "your-16-24-32-byte-encryption-key"
}
```

### Environment Variables
```powershell
# Set in IIS Application Pool or web.config
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

## Security Notes

- All encryption uses AES with SHA256 key derivation
- License key format: `yyyy-MM-dd HH:mm:ss`
- Encrypted values are Base64 encoded
- Use HTTPS in production
- Consider adding authentication for production use
