USE [Timeoffice]
GO

-- Create PunchData table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PunchData]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PunchData] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(255) NOT NULL,
        [Empcode] NVARCHAR(50) NOT NULL,
        [PunchDate] DATETIME NOT NULL,
        [M_Flag] NVARCHAR(10) NOT NULL,
        [CreatedDate] DATETIME DEFAULT GETDATE(),
        [SyncDate] DATETIME DEFAULT GETDATE()
    );

    -- Create index on Empcode for faster queries
    CREATE INDEX IX_PunchData_Empcode ON [dbo].[PunchData] ([Empcode]);
    
    -- Create index on PunchDate for date range queries
    CREATE INDEX IX_PunchData_PunchDate ON [dbo].[PunchData] ([PunchDate]);
END
GO
