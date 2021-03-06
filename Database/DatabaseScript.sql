USE [EmployDB]
GO
/****** Object:  Table [dbo].[Tbl_Link]    Script Date: 4/25/2021 8:29:50 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tbl_Link](
	[Id] [nvarchar](50) NOT NULL,
	[ExpireDate] [datetime] NULL,
	[Phone] [nvarchar](50) NULL,
 CONSTRAINT [PK_Tbl_Link] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Tbl_SmsReceived]    Script Date: 4/25/2021 8:29:50 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tbl_SmsReceived](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Phone] [nvarchar](20) NULL,
	[Date] [datetime] NULL,
	[Message] [nvarchar](10) NULL,
 CONSTRAINT [PK_SmsReceived] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Tbl_SmsSent]    Script Date: 4/25/2021 8:29:50 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tbl_SmsSent](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Phone] [nvarchar](20) NULL,
	[Date] [datetime] NULL,
	[Message] [nvarchar](100) NULL,
 CONSTRAINT [PK_SmsSent] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
