-- =============================================
-- 시스템 전체의 중요 이벤트를 기록하는 감사 로그
-- 규정 준수 및 보안 사고 조사에 활용
-- =============================================
CREATE TABLE AuditLogs (
    AuditId         INT             PRIMARY KEY IDENTITY(1,1),
    EventType       NVARCHAR(100)   NOT NULL, -- ClientRegistered, KeyGenerated, KeyAccessed, KeyRevoked, etc.
    Severity        NVARCHAR(20)    NOT NULL DEFAULT 'Info', -- Info, Warning, Error, Critical
    ResourceType    NVARCHAR(50), -- Client, Key, System
    ResourceId      NVARCHAR(100),
    Actor           NVARCHAR(100)   NOT NULL, -- 작업을 수행한 주체
    ActorIP         NVARCHAR(50),
    ActionResult    NVARCHAR(20)    NOT NULL, -- Success, Failed, Denied
    Details         NVARCHAR(MAX),
    Timestamp       DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Severity CHECK (Severity IN ('Info', 'Warning', 'Error', 'Critical')),
    CONSTRAINT CK_ActionResult CHECK (ActionResult IN ('Success', 'Failed', 'Denied'))
);

CREATE INDEX IX_AuditLogs_EventType ON AuditLogs(EventType);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_AuditLogs_Severity ON AuditLogs(Severity);
CREATE INDEX IX_AuditLogs_Actor ON AuditLogs(Actor);
GO

