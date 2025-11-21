-- =============================================
-- 클라이언트별 요청 제한을 관리하는 테이블
-- 과도한 요청으로 인한 서비스 거부 공격 방지
-- =============================================
CREATE TABLE RateLimits (
    LimitId             INT             PRIMARY KEY IDENTITY(1,1),
    RequestIP           NVARCHAR(50)    NOT NULL,
    RequestTimestamp    DATETIME2       NOT NULL DEFAULT GETDATE(),
    RequestCount        INT             NOT NULL DEFAULT 1,
    IsBlocked           BIT             NOT NULL DEFAULT 0,
    BlockedUntil        DATETIME2
);

CREATE INDEX IX_RateLimits_IP_Timestamp ON RateLimits(RequestIP, RequestTimestamp);
CREATE INDEX IX_RateLimits_IsBlocked ON RateLimits(IsBlocked, BlockedUntil);
GO