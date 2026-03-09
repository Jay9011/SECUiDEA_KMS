CREATE TABLE [dbo].[ClientServers] (
    [ClientId]         INT              IDENTITY (1, 1) NOT NULL,
    [ClientGuid]       UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [ClientName]       NVARCHAR (200)   NOT NULL,
    [ClientIP]         NVARCHAR (50)    NOT NULL,
    [IPValidationMode] NVARCHAR (50)    DEFAULT ('Strict') NOT NULL,
    [Description]      NVARCHAR (500)   NULL,
    [IsActive]         BIT              DEFAULT ((1)) NOT NULL,
    [CreatedAt]        DATETIME2 (7)    DEFAULT (getdate()) NOT NULL,
    [CreatedBy]        NVARCHAR (100)   NOT NULL,
    [ModifiedAt]       DATETIME2 (7)    NULL,
    [ModifiedBy]       NVARCHAR (100)   NULL,
    [LastAccessAt]     DATETIME2 (7)    NULL,
    [LastAccessIP]     NVARCHAR (50)    NULL,
    PRIMARY KEY CLUSTERED ([ClientId] ASC),
    CONSTRAINT [CK_ClientIP_NotEmpty] CHECK (len([ClientIP])>(0)),
    CONSTRAINT [CK_ClientName_NotEmpty] CHECK (len([ClientName])>(0)),
    UNIQUE NONCLUSTERED ([ClientGuid] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_ClientServers_ClientGuid]
    ON [dbo].[ClientServers]([ClientGuid] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClientServers_ClientIP]
    ON [dbo].[ClientServers]([ClientIP] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClientServers_IsActive]
    ON [dbo].[ClientServers]([IsActive] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ClientServers_LastAccessAt]
    ON [dbo].[ClientServers]([LastAccessAt] ASC);

