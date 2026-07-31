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
    └── CreateAttenifoTable.sql
```

## Database Setup

Run these SQL scripts on target database to create tables:

### ApiLog Table
```sql
CREATE TABLE [dbo].[ApiLog](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [RequestUrl] [nvarchar](500) NOT NULL,
    [RequestTime] [datetime] NOT NULL,
    [Status] [nvarchar](10) NOT NULL,
    [RecordsCount] [int] NULL,
    [ExceptionMsg] [nvarchar](max) NULL,
    [CreatedDate] [datetime] NULL,
PRIMARY KEY CLUSTERED ([Id] ASC)
)

ALTER TABLE [dbo].[ApiLog] ADD DEFAULT ((0)) FOR [RecordsCount]
ALTER TABLE [dbo].[ApiLog] ADD DEFAULT (getdate()) FOR [CreatedDate]
```

### ApiSyncStatus Table
```sql
CREATE TABLE [dbo].[ApiSyncStatus](
    [Id] [int] NOT NULL,
    [LastSyncTime] [datetime] NULL,
    [CreatedDate] [datetime] NULL,
    [ModifiedDate] [datetime] NULL,
PRIMARY KEY CLUSTERED ([Id] ASC)
)

ALTER TABLE [dbo].[ApiSyncStatus] ADD DEFAULT ((1)) FOR [Id]
ALTER TABLE [dbo].[ApiSyncStatus] ADD DEFAULT (getdate()) FOR [CreatedDate]
ALTER TABLE [dbo].[ApiSyncStatus] ADD DEFAULT (getdate()) FOR [ModifiedDate]
```

### Attenifo Table
```sql
CREATE TABLE [dbo].[Attenifo](
    [Srno] [int] IDENTITY(1,1) NOT NULL,
    [EmpCode] [nvarchar](50) NULL,
    [TicketNo] [nvarchar](50) NULL,
    [EntryDate] [date] NOT NULL,
    [InOutFlag] [nvarchar](10) NULL,
    [EntryTime] [time](7) NOT NULL,
    [TrfFlag] [nvarchar](10) NULL,
    [UpdateUID] [nvarchar](100) NULL,
    [Location] [nvarchar](100) NULL,
    [ErrMsg] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED ([Srno] ASC)
)

CREATE INDEX IX_Attenifo_EmpCode ON [dbo].[Attenifo] ([EmpCode])
CREATE INDEX IX_Attenifo_EntryDate ON [dbo].[Attenifo] ([EntryDate])
```

## Tables Description

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
