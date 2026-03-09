CREATE TABLE [dbo].[AuditLogs] (
    [AuditId]      INT            IDENTITY (1, 1) NOT NULL,
    [EventType]    NVARCHAR (100) NOT NULL,
    [Severity]     NVARCHAR (20)  DEFAULT ('Info') NOT NULL,
    [ResourceType] NVARCHAR (50)  NULL,
    [ResourceId]   NVARCHAR (100) NULL,
    [Actor]        NVARCHAR (100) NOT NULL,
    [ActorIP]      NVARCHAR (50)  NULL,
    [ActionResult] NVARCHAR (20)  NOT NULL,
    [Details]      NVARCHAR (MAX) NULL,
    [Timestamp]    DATETIME2 (7)  DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([AuditId] ASC),
    CONSTRAINT [CK_ActionResult] CHECK ([ActionResult]='Denied' OR [ActionResult]='Failed' OR [ActionResult]='Success'),
    CONSTRAINT [CK_Severity] CHECK ([Severity]='Critical' OR [Severity]='Error' OR [Severity]='Warning' OR [Severity]='Info')
);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_EventType]
    ON [dbo].[AuditLogs]([EventType] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Timestamp]
    ON [dbo].[AuditLogs]([Timestamp] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Severity]
    ON [dbo].[AuditLogs]([Severity] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Actor]
    ON [dbo].[AuditLogs]([Actor] ASC);

