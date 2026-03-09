CREATE TABLE [dbo].[EncryptionKeys] (
    [KeyId]                INT             IDENTITY (1, 1) NOT NULL,
    [ClientId]             INT             NOT NULL,
    [EncryptedKeyData]     VARBINARY (MAX) NOT NULL,
    [KeyVersion]           INT             DEFAULT ((1)) NOT NULL,
    [KeyStatus]            NVARCHAR (20)   DEFAULT ('Active') NOT NULL,
    [IsAutoRotation]       BIT             DEFAULT ((0)) NOT NULL,
    [RotationScheduleDays] INT             NULL,
    [CreatedAt]            DATETIME2 (7)   DEFAULT (getdate()) NOT NULL,
    [ExpiresAt]            DATETIME2 (7)   NOT NULL,
    [RevokedAt]            DATETIME2 (7)   NULL,
    [RevokedReason]        NVARCHAR (500)  NULL,
    PRIMARY KEY CLUSTERED ([KeyId] ASC),
    CONSTRAINT [CK_ExpiresAt_Future] CHECK ([ExpiresAt]>[CreatedAt]),
    CONSTRAINT [CK_KeyStatus] CHECK ([KeyStatus]='Revoked' OR [KeyStatus]='Expired' OR [KeyStatus]='Active'),
    CONSTRAINT [CK_RotationSchedule] CHECK ([IsAutoRotation]=(0) AND [RotationScheduleDays] IS NULL OR [IsAutoRotation]=(1) AND [RotationScheduleDays]>(0)),
    CONSTRAINT [FK_EncryptionKeys_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[ClientServers] ([ClientId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_EncryptionKeys_ClientId_Status]
    ON [dbo].[EncryptionKeys]([ClientId] ASC, [KeyStatus] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_EncryptionKeys_ExpiresAt]
    ON [dbo].[EncryptionKeys]([ExpiresAt] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_EncryptionKeys_KeyVersion]
    ON [dbo].[EncryptionKeys]([ClientId] ASC, [KeyVersion] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_EncryptionKeys_IsAutoRotation]
    ON [dbo].[EncryptionKeys]([IsAutoRotation] ASC, [ExpiresAt] ASC);

