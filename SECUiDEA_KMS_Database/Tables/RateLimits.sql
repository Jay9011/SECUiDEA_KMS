CREATE TABLE [dbo].[RateLimits] (
    [LimitId]          INT           IDENTITY (1, 1) NOT NULL,
    [RequestIP]        NVARCHAR (50) NOT NULL,
    [RequestTimestamp] DATETIME2 (7) DEFAULT (getdate()) NOT NULL,
    [RequestCount]     INT           DEFAULT ((1)) NOT NULL,
    [IsBlocked]        BIT           DEFAULT ((0)) NOT NULL,
    [BlockedUntil]     DATETIME2 (7) NULL,
    PRIMARY KEY CLUSTERED ([LimitId] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_RateLimits_IP_Timestamp]
    ON [dbo].[RateLimits]([RequestIP] ASC, [RequestTimestamp] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_RateLimits_IsBlocked]
    ON [dbo].[RateLimits]([IsBlocked] ASC, [BlockedUntil] ASC);

