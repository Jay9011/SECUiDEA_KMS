CREATE TABLE [dbo].[RateLimitsSettings] (
    [RateLimitSeconds] INT            DEFAULT ((60)) NOT NULL,
    [MaxRequests]      INT            DEFAULT ((100)) NOT NULL,
    [IsEnabled]        BIT            DEFAULT ((1)) NOT NULL,
    [CreatedAt]        DATETIME2 (7)  DEFAULT (getdate()) NOT NULL,
    [CreatedBy]        NVARCHAR (100) NOT NULL,
    [ModifiedAt]       DATETIME2 (7)  NULL,
    [ModifiedBy]       NVARCHAR (100) NULL,
    CONSTRAINT [CK_RateLimitsSettings_MaxRequests] CHECK ([MaxRequests]>(0)),
    CONSTRAINT [CK_RateLimitsSettings_RateLimitSeconds] CHECK ([RateLimitSeconds]>(0))
);


GO
CREATE NONCLUSTERED INDEX [IX_RateLimitsSettings_IsEnabled]
    ON [dbo].[RateLimitsSettings]([IsEnabled] ASC);

