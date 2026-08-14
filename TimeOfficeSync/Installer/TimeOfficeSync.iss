[Setup]
AppName=SoftoLogsync
AppVersion=1.0
DefaultDirName={pf}\SoftoLogsync
DefaultGroupName=SoftoLogsync
OutputDir=D:\Installer\Output
OutputBaseFilename=SoftoLogsyncSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Files]
Source: "D:\Projects\timeoffice\publishfile\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion restartreplace

[Run]

; ================================
; LOG START 
; ================================
Filename: "cmd.exe"; Parameters: "/c echo ===== INSTALL START ===== > C:\SoftoLogsyncInstallLog.txt"; Flags: runhidden

; ================================
; SERVICE SETUP
; ================================
Filename: "cmd.exe"; Parameters: "/c sc stop SoftoLogsync >> C:\SoftoLogsyncInstallLog.txt 2>&1"; Flags: runhidden
Filename: "cmd.exe"; Parameters: "/c sc delete SoftoLogsync >> C:\SoftoLogsyncInstallLog.txt 2>&1"; Flags: runhidden

Filename: "cmd.exe"; \
Parameters: "/c sc create SoftoLogsync binPath= ""{app}\TimeOfficeSync.exe"" start= auto >> C:\SoftoLogsyncInstallLog.txt 2>&1"; \
Flags: runhidden

Filename: "cmd.exe"; \
Parameters: "/c sc description SoftoLogsync ""SoftoLogsync - Syncs punch data from ETimeOffice API"" >> C:\SoftoLogsyncInstallLog.txt 2>&1"; \
Flags: runhidden

Filename: "cmd.exe"; Parameters: "/c timeout /t 3 >nul"; Flags: runhidden

Filename: "cmd.exe"; \
Parameters: "/c sc start SoftoLogsync >> C:\SoftoLogsyncInstallLog.txt 2>&1"; \
Flags: runhidden

; ================================
; LOG END
; ================================
Filename: "cmd.exe"; Parameters: "/c echo ===== INSTALL END ===== >> C:\SoftoLogsyncInstallLog.txt"; Flags: runhidden

[UninstallRun]
Filename: "cmd.exe"; Parameters: "/c sc stop SoftoLogsync"; Flags: runhidden waituntilterminated
Filename: "cmd.exe"; Parameters: "/c sc delete SoftoLogsync"; Flags: runhidden waituntilterminated
