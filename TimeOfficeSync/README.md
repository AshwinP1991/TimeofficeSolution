# TimeOfficeSync - SOFTO ALERT

Windows service that syncs employee attendance punch data from ETimeOffice API to SQL Server database.

## Features

- Auto sync punch data from ETimeOffice API
- Runs as Windows service
- Configurable sync interval
- License expiry with email notification
- API request logging
- Duplicate record prevention

## Project Structure

```
TimeOfficeSync/
├── Services/
│   ├── ApiService.cs          # API calls with Basic Auth
│   ├── DatabaseService.cs     # SQL Server operations
│   ├── EmailService.cs        # Email notifications
│   └── LicenseService.cs      # License validation
├── Models/
│   ├── ApiResponse.cs         # API response models
│   └── PunchDataEntity.cs     # Database entity
├── Worker.cs                  # Background service
├── Program.cs                 # Entry point
├── appsettings.json           # Configuration
└── Scripts/
    ├── CreateAttenifoTable.sql
    └── CreatePunchDataTable.sql
```

## Database Tables

### Attenifo
| Column | Type | Description |
|--------|------|-------------|
| Srno | INT | Auto generated |
| EmpCode | NVARCHAR(50) | Employee code |
| EntryDate | DATE | Punch date |
| EntryTime | TIME | Punch time |
| InOutFlag | NVARCHAR(10) | I/O/Z |

### ApiLog
| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Auto generated |
| RequestUrl | NVARCHAR(500) | API URL |
| RequestTime | DATETIME | Request time |
| Status | NVARCHAR(10) | Success/Fail |
| RecordsCount | INT | Records fetched |
| ExceptionMsg | NVARCHAR(MAX) | Error message |

### ApiSyncStatus
| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Always 1 |
| LastSyncTime | DATETIME | Last successful sync |

## Configuration (appsettings.json)

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.etimeoffice.com/api/DownloadPunchData",
    "Username": "your-api-credentials",
    "FromDateDaysBack": 30
  },
  "DatabaseSettings": {
    "ConnectionString": "Server=...;Database=...;User Id=...;Password=..."
  },
  "ScheduleSettings": {
    "Enabled": true,
    "RunOnceNow": false,
    "RunIntervalMinutes": 5
  },
  "LicenseSettings": {
    "LicenseKey": "encrypted-license-key"
  },
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "smtp.office365.com",
    "Port": 587,
    "EncryptedUsername": "encrypted-email",
    "EncryptedPassword": "encrypted-password",
    "From": "info@softovista.com",
    "DisplayName": "SOFTO ALERT",
    "To": "recipient@company.com",
    "CC": "cc1@company.com, cc2@company.com",
    "ClientName": "Client Name"
  }
}
```

## Schedule Modes

### Interval Mode (Recommended)
```json
"ScheduleSettings": {
  "Enabled": true,
  "RunOnceNow": false,
  "RunIntervalMinutes": 5
}
```
Runs every 5 minutes.

### Run Once Now
```json
"ScheduleSettings": {
  "RunOnceNow": true,
  "RunIntervalMinutes": 5
}
```
Runs immediately, then every 5 minutes.

## Build & Publish

```powershell
# Build
dotnet build

# Publish
dotnet publish -c Release -r win-x64 --self-contained -o bin\Release\net8.0\win-x64\publish
```

## Install as Windows Service

### Manual Install
```powershell
# Create service
sc create SoftoLogsync binPath="C:\Path\To\TimeOfficeSync.exe" start=auto

# Set description
sc description SoftoLogsync "SoftoLogsync - Syncs punch data from ETimeOffice API"

# Start service
sc start SoftoLogsync
```

### Using Installer
1. Open Inno Setup Compiler
2. Open `Installer\TimeOfficeSync.iss`
3. Build → Compile
4. Run `SoftoLogsyncSetup.exe`

## Service Commands

```powershell
sc start SoftoLogsync    # Start
sc stop SoftoLogsync     # Stop
sc delete SoftoLogsync   # Uninstall
```

## License System

- License key is AES encrypted expiry date
- Stored in `appsettings.json`
- Checked before each sync
- Email sent when expired

### Generate License Key
```powershell
cd LicenseGen
dotnet run -- license "2026-12-31 23:59:59"
```

## Email Notifications

- Sent when license expires
- Includes: Client name, expiry date, server name
- Sent to: To and CC addresses

## Troubleshooting

### Service not starting
1. Check Event Viewer → Windows Logs → Application
2. Check `C:\SoftoLogsyncInstallLog.txt`

### No data syncing
1. Check `services.msc` - service should be Running
2. Check `ApiLog` table for errors
3. Verify database connection string

### Tables not created
Run SQL scripts in `Scripts/` folder on target database.

## Logs

- **Windows Event Viewer**: Application logs
- **ApiLog table**: API request/response logs
- **LicenseLog**: License check logs (in ApiLog table)
