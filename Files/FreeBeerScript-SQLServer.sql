USE [FreeBeerdbTest]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 12/8/2022 6:42:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MoneyType]    Script Date: 12/8/2022 6:42:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MoneyType](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [int] NOT NULL,
 CONSTRAINT [PK_MoneyType] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Player]    Script Date: 12/8/2022 6:42:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Player](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PlayerName] [varchar](50) NULL,
	[PlayerId] [varchar](50) NULL,
 CONSTRAINT [PK_Player] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlayerLoot]    Script Date: 12/8/2022 6:42:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlayerLoot](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TypeID] [int] NOT NULL,
	[PlayerID] [int] NOT NULL,
	[Loot] [decimal](18, 0) NOT NULL,
	[CreateDate] [datetime] NULL,
	[Reason] [varchar](250) NOT NULL,
	[PartyLeader] [varchar](250) NOT NULL,
	[KillId] [varchar](250) NOT NULL,
	[QueueId] [varchar](250) NOT NULL,
	[Message] [varchar](250) NOT NULL,
 CONSTRAINT [PK_Person] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[PlayerLoot] ADD  CONSTRAINT [DF__PlayerLoo__Creat__3C69FB99]  DEFAULT (getdate()) FOR [CreateDate]
GO
ALTER TABLE [dbo].[PlayerLoot]  WITH CHECK ADD  CONSTRAINT [FK__PlayerLoo__Playe__29572725] FOREIGN KEY([PlayerID])
REFERENCES [dbo].[Player] ([id])
GO
ALTER TABLE [dbo].[PlayerLoot] CHECK CONSTRAINT [FK__PlayerLoo__Playe__29572725]
GO
ALTER TABLE [dbo].[PlayerLoot]  WITH CHECK ADD  CONSTRAINT [FK__PlayerLoo__TypeI__286302EC] FOREIGN KEY([TypeID])
REFERENCES [dbo].[MoneyType] ([id])
GO
ALTER TABLE [dbo].[PlayerLoot] CHECK CONSTRAINT [FK__PlayerLoo__TypeI__286302EC]
GO
