-- =============================================
-- 요청 제한 설정을 관리하는 테이블
-- =============================================
CREATE TABLE RateLimitsSettings (
    RateLimitSeconds    INT             NOT NULL DEFAULT 60,
    MaxRequests         INT             NOT NULL DEFAULT 100,
    IsEnabled           BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           NVARCHAR(100)   NOT NULL,
    ModifiedAt          DATETIME2,
    ModifiedBy          NVARCHAR(100),
    CONSTRAINT CK_RateLimitsSettings_RateLimitSeconds CHECK (RateLimitSeconds > 0),
    CONSTRAINT CK_RateLimitsSettings_MaxRequests CHECK (MaxRequests > 0)
);

CREATE INDEX IX_RateLimitsSettings_IsEnabled ON RateLimitsSettings(IsEnabled);
GO