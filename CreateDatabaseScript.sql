USE [SysLogDB]
GO
/****** Object:  StoredProcedure [dbo].[NewLog]    Script Date: 08/20/2014 16:49:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Virvent, Inc.
-- Create date: 
-- Description:	
-- =============================================
CREATE PROCEDURE [dbo].[NewLog] 
	-- Add the parameters for the stored procedure here
            @timestamp datetime 
           ,@sourceip char(10)
           ,@sourcename varchar(150)
           ,@severity int
           ,@facility int
           ,@version int
           ,@hostname varchar(255)
           ,@appname varchar(48)
           ,@procid varchar(128)
           ,@msgid varchar(32)
           ,@msgtimestamp datetime
           ,@msgoffset char(6)
           ,@sdid bigint=0
           ,@msg text

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
INSERT INTO [dbo].[logs]
		   ([timestamp]
           ,[sourceip]
           ,[sourcename]
           ,[severity]
           ,[facility]
           ,[version]
           ,[hostname]
           ,[appname]
           ,[procid]
           ,[msgid]
           ,[msgtimestamp]
           ,[msgoffset]
           ,[sdid]
           ,[msg])
     VALUES
           (@timestamp
           ,@sourceip
           ,@sourcename
           ,@severity
           ,@facility
           ,@version
           ,@hostname
           ,@appname
           ,@procid
           ,@msgid
           ,@msgtimestamp
           ,@msgoffset
           ,@sdid
           ,@msg)
END
GO
/****** Object:  Table [dbo].[logs]    Script Date: 08/20/2014 16:48:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[logs](
	[recordnumber] [bigint] IDENTITY(1,1) NOT NULL,
	[timestamp] [datetime] NOT NULL,
	[sourceip] [char](10) NOT NULL,
	[sourcename] [varchar](150) NOT NULL,
	[severity] [int] NOT NULL,
	[facility] [int] NOT NULL,
	[version] [int] NULL,
	[hostname] [varchar](255) NULL,
	[appname] [varchar](48) NULL,
	[procid] [varchar](128) NULL,
	[msgid] [varchar](32) NULL,
	[msgtimestamp] [datetime] NULL,
	[msgoffset] [char](6) NULL,
	[sdid] [bigint] NULL,
	[msg] [text] NULL,
 CONSTRAINT [PK_logs] PRIMARY KEY CLUSTERED 
(
	[recordnumber] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  UserDefinedFunction [dbo].[SeverityName]    Script Date: 08/20/2014 16:49:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[SeverityName] 
(
	-- Add the parameters for the function here
	@Severity int
)
RETURNS nvarchar(40)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @str nvarchar(40)

	-- Add the T-SQL statements to compute the return value here
	SELECT @str = case @Severity
		when 0	then 'Emergency'
        when 1	then 'Alert'
        when 2	then 'Critical'
        when 3	then 'Error'
        when 4	then 'Warning'
        when 5	then 'Notice'
        when 6	then 'Informational'
        when 7	then 'Debug'
        Else 'Unknown'
        End
	-- Return the result of the function
	RETURN @str

END
GO
/****** Object:  UserDefinedFunction [dbo].[FacilityName]    Script Date: 08/20/2014 16:49:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[FacilityName] 
(
	-- Add the parameters for the function here
	@Facility int
)
RETURNS nvarchar(40)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @str nvarchar(40)

	-- Add the T-SQL statements to compute the return value here
	SELECT @str = case @Facility
		when 0	then 'kernel_messages'
        when 1	then 'user_level_messages'
        when 2	then 'mail_system'
        when 3	then 'system_daemons'
        when 4	then 'security_authorization_messages'
        when 5	then 'messages_generated_internally_by_syslogd'
        when 6	then 'line_printer_subsystem'
        when 7	then 'network_news_subsystem'
        when 8	then 'UUCP_subsystem'
        when 9	then 'clock_daemon'
        when 10	then 'security_authorization_messages1'
        when 11	then 'FTP_daemon'
        when 12	then 'NTP_subsystem'
        when 13	then 'log_audit'
        when 14	then 'log_alert'
        when 15	then 'clock_daemon_note_2'
        when 16	then 'local_use_0__local0'
        when 17	then 'local_use_1__local1'
        when 18	then 'local_use_2__local2'
        when 19	then 'local_use_3__local3'
        when 20	then 'local_use_4__local4'
        when 21	then 'local_use_5__local5'
        when 22	then 'local_use_6__local6'
        when 23	then 'local_use_7__local7'
        Else 'Unknown'
        End
	-- Return the result of the function
	RETURN @str

END
GO
/****** Object:  Table [dbo].[nextsdid]    Script Date: 08/20/2014 16:48:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[nextsdid](
	[id] [bigint] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[structuredData]    Script Date: 08/20/2014 16:48:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[structuredData](
	[sdlogid] [bigint] NOT NULL,
	[SDID] [char](32) NOT NULL,
	[ParamName] [char](32) NOT NULL,
	[paramValue] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_structuredData] PRIMARY KEY CLUSTERED 
(
	[sdlogid] ASC,
	[SDID] ASC,
	[ParamName] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  View [dbo].[Logs_v]    Script Date: 08/20/2014 16:49:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[Logs_v]
AS
SELECT     recordnumber, timestamp, sourceip, sourcename, dbo.SeverityName(severity) AS Severity, dbo.FacilityName(facility) AS Facility, version, hostname, appname, procid, 
                      msgid, msgtimestamp, msgoffset, sdid, CAST(msg AS NVARCHAR(MAX)) AS msg
FROM         dbo.logs
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "logs"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 334
               Right = 198
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 16
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'Logs_v'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'Logs_v'
GO
/****** Object:  StoredProcedure [dbo].[NewSD]    Script Date: 08/20/2014 16:49:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Laca
-- Create date: 
-- Description:	
-- =============================================
CREATE PROCEDURE [dbo].[NewSD] 
	-- Add the parameters for the stored procedure here
	@sdid char(32) = '-', 
	@sdpn char(32) = '-',
	@sdpv nvarchar(max) = N'-'
AS
BEGIN
declare @nsdid bigint
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    if  not exists(select id from dbo.nextsdid) insert into dbo.nextsdid values(1)
    select @nsdid=id from dbo.nextsdid
	update dbo.nextsdid set id=@nsdid+1
    insert into dbo.structureddata values(@nsdid,@sdid,@sdpn,@sdpv)
    return @nsdid
END
GO
/****** Object:  StoredProcedure [dbo].[NextSD]    Script Date: 08/20/2014 16:49:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Laca
-- Create date: 
-- Description:	
-- =============================================
CREATE PROCEDURE [dbo].[NextSD] 
	-- Add the parameters for the stored procedure here
	@nsdid bigint,
	@sdid char(32) = '-', 
	@sdpn char(32) = '-',
	@sdpv nvarchar(max) = N'-'
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    insert into dbo.structureddata values(@nsdid,@sdid,@sdpn,@sdpv)
END
GO
