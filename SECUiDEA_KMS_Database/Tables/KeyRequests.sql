CREATE TABLE [dbo].[KeyRequests] (
    [RequestId]        INT            IDENTITY (1, 1) NOT NULL,
    [ClientId]         INT            NOT NULL,
    [KeyId]            INT            NULL,
    [Operation]        NVARCHAR (50)  NOT NULL,
    [RequestIP]        NVARCHAR (50)  NULL,
    [RequestUserAgent] NVARCHAR (500) NULL,
    [RequestHost]      NVARCHAR (200) NULL,
    [RequestPath]      NVARCHAR (500) NULL,
    [Success]          BIT            NOT NULL,
    [ErrorCode]        NVARCHAR (50)  NULL,
    [ErrorMessage]     NVARCHAR (MAX) NULL,
    [RequestTimestamp] DATETIME2 (7)  DEFAULT (getdate()) NOT NULL,
    [ResponseTimeMs]   INT            NULL,
    PRIMARY KEY CLUSTERED ([RequestId] ASC),
    CONSTRAINT [FK_KeyRequests_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[ClientServers] ([ClientId]),
    CONSTRAINT [FK_KeyRequests_KeyId] FOREIGN KEY ([KeyId]) REFERENCES [dbo].[EncryptionKeys] ([KeyId])
);


GO
CREATE NONCLUSTERED INDEX [IX_KeyRequests_ClientId]
    ON [dbo].[KeyRequests]([ClientId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_KeyRequests_Timestamp]
    ON [dbo].[KeyRequests]([RequestTimestamp] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_KeyRequests_Success]
    ON [dbo].[KeyRequests]([Success] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_KeyRequests_Operation]
    ON [dbo].[KeyRequests]([Operation] ASC);

