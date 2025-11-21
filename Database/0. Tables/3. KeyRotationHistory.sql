-- =============================================
-- 키 회전 이력을 추적하는 테이블
-- 이전 버전의 키를 일정 기간 보관하여 복호화 지원
-- =============================================
CREATE TABLE KeyRotationHistory (
    RotationId      INT             PRIMARY KEY IDENTITY(1,1),
    ClientId        INT             NOT NULL,
    OldKeyId        INT             NOT NULL,
    NewKeyId        INT             NOT NULL,
    RotationType    NVARCHAR(50)    NOT NULL, -- Manual, Scheduled, Forced
    RotatedBy       NVARCHAR(100)   NOT NULL,
    RotatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    Reason          NVARCHAR(500),
    CONSTRAINT FK_KeyRotationHistory_ClientId FOREIGN KEY (ClientId) 
        REFERENCES ClientServers(ClientId),
    CONSTRAINT FK_KeyRotationHistory_OldKeyId FOREIGN KEY (OldKeyId) 
        REFERENCES EncryptionKeys(KeyId),
    CONSTRAINT FK_KeyRotationHistory_NewKeyId FOREIGN KEY (NewKeyId) 
        REFERENCES EncryptionKeys(KeyId)
);

CREATE INDEX IX_KeyRotationHistory_ClientId ON KeyRotationHistory(ClientId);
CREATE INDEX IX_KeyRotationHistory_RotatedAt ON KeyRotationHistory(RotatedAt);
GO

