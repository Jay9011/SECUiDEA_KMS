CREATE TABLE [dbo].[KeyRotationHistory] (
    [RotationId]   INT            IDENTITY (1, 1) NOT NULL,
    [ClientId]     INT            NOT NULL,
    [OldKeyId]     INT            NOT NULL,
    [NewKeyId]     INT            NOT NULL,
    [RotationType] NVARCHAR (50)  NOT NULL,
    [RotatedBy]    NVARCHAR (100) NOT NULL,
    [RotatedAt]    DATETIME2 (7)  DEFAULT (getdate()) NOT NULL,
    [Reason]       NVARCHAR (500) NULL,
    PRIMARY KEY CLUSTERED ([RotationId] ASC),
    CONSTRAINT [FK_KeyRotationHistory_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[ClientServers] ([ClientId]),
    CONSTRAINT [FK_KeyRotationHistory_NewKeyId] FOREIGN KEY ([NewKeyId]) REFERENCES [dbo].[EncryptionKeys] ([KeyId]),
    CONSTRAINT [FK_KeyRotationHistory_OldKeyId] FOREIGN KEY ([OldKeyId]) REFERENCES [dbo].[EncryptionKeys] ([KeyId])
);


GO
CREATE NONCLUSTERED INDEX [IX_KeyRotationHistory_ClientId]
    ON [dbo].[KeyRotationHistory]([ClientId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_KeyRotationHistory_RotatedAt]
    ON [dbo].[KeyRotationHistory]([RotatedAt] ASC);

