USE [Timeoffice]
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Attenifo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Attenifo] (
	[Srno] [int] IDENTITY(1,1) NOT NULL,
	[EmpCode] [nvarchar](50) NULL,
	[TicketNo] [nvarchar](50) NULL,
	[EntryDate] [datetime] NOT NULL,
	[InOutFlag] [nvarchar](10) NULL,
	[EntryTime] [datetime] NULL,
	[TrfFlag] [nvarchar](10) NULL,
	[UpdateUID] [nvarchar](100) NULL,
	[Location] [nvarchar](100) NULL,
	[ErrMsg] [nvarchar](255) NULL,
	[first_name] [nvarchar](200) NULL,
	[location_sublocation_unit] [nvarchar](200) NULL,
	[device_ip] [nvarchar](200) NULL,
    );

    CREATE INDEX IX_Attenifo_EmpCode ON [dbo].[Attenifo] ([EmpCode]);
    CREATE INDEX IX_Attenifo_EntryDate ON [dbo].[Attenifo] ([EntryDate]);
END
GO
