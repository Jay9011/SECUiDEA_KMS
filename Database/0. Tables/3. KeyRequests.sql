-- =============================================
-- 모든 키 요청을 기록하는 로그 테이블
-- 보안 감사 및 사용량 분석에 활용
-- =============================================
CREATE TABLE KeyRequests (
    RequestId           INT             PRIMARY KEY IDENTITY(1,1),
    ClientId            INT             NOT NULL,
    KeyId               INT,
    Operation           NVARCHAR(50)    NOT NULL, -- GetKey, GenerateKey, RotateKey, RevokeKey
    RequestIP           NVARCHAR(50),
    RequestUserAgent    NVARCHAR(500),
    RequestHost         NVARCHAR(200),
    RequestPath         NVARCHAR(500),
    Success             BIT             NOT NULL,
    ErrorCode           NVARCHAR(50),
    ErrorMessage        NVARCHAR(MAX),
    RequestTimestamp    DATETIME2       NOT NULL DEFAULT GETDATE(),
    ResponseTimeMs      INT,
    CONSTRAINT FK_KeyRequests_ClientId FOREIGN KEY (ClientId) 
        REFERENCES ClientServers(ClientId),
    CONSTRAINT FK_KeyRequests_KeyId FOREIGN KEY (KeyId) 
        REFERENCES EncryptionKeys(KeyId)
);

CREATE INDEX IX_KeyRequests_ClientId ON KeyRequests(ClientId);
CREATE INDEX IX_KeyRequests_Timestamp ON KeyRequests(RequestTimestamp);
CREATE INDEX IX_KeyRequests_Success ON KeyRequests(Success);
CREATE INDEX IX_KeyRequests_Operation ON KeyRequests(Operation);
GO

