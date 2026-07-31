USE [Timeoffice]
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Attenifo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Attenifo] (
        [Srno] INT IDENTITY(1,1) PRIMARY KEY,
        [EmpCode] NVARCHAR(50) NULL,
        [TicketNo] NVARCHAR(50) NULL,
        [EntryDate] DATE NOT NULL,
        [InOutFlag] NVARCHAR(10) NULL,
        [EntryTime] TIME(7) NOT NULL,
        [TrfFlag] NVARCHAR(10) NULL,
        [UpdateUID] NVARCHAR(100) NULL,
        [Location] NVARCHAR(100) NULL,
        [ErrMsg] NVARCHAR(255) NULL
    );

    CREATE INDEX IX_Attenifo_EmpCode ON [dbo].[Attenifo] ([EmpCode]);
    CREATE INDEX IX_Attenifo_EntryDate ON [dbo].[Attenifo] ([EntryDate]);
END
GO
