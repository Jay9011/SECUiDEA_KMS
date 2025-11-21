-- =============================================
-- 클라이언트별 암호화 키를 저장하는 핵심 테이블
-- 마스터 키로 암호화된 상태로 저장되며, 버전 관리 지원
-- =============================================
CREATE TABLE EncryptionKeys (
    KeyId                   INT             PRIMARY KEY IDENTITY(1,1),
    ClientId                INT             NOT NULL,
    EncryptedKeyData        VARBINARY(MAX)  NOT NULL, -- 마스터 키로 암호화된 AES 키
    KeyVersion              INT             NOT NULL DEFAULT 1,
    KeyStatus               NVARCHAR(20)    NOT NULL DEFAULT 'Active', -- Active, Expired, Revoked
    IsAutoRotation          BIT             NOT NULL DEFAULT 0, -- 자동 회전 여부 (0: 반영구적, 1: 자동 회전)
    RotationScheduleDays    INT,
    CreatedAt               DATETIME2       NOT NULL DEFAULT GETDATE(),
    ExpiresAt               DATETIME2       NOT NULL,
    RevokedAt               DATETIME2,
    RevokedReason           NVARCHAR(500),
    CONSTRAINT FK_EncryptionKeys_ClientId FOREIGN KEY (ClientId) 
        REFERENCES ClientServers(ClientId) ON DELETE CASCADE,
    CONSTRAINT CK_KeyStatus CHECK (KeyStatus IN ('Active', 'Expired', 'Revoked')),
    CONSTRAINT CK_ExpiresAt_Future CHECK (ExpiresAt > CreatedAt),
    CONSTRAINT CK_RotationSchedule CHECK (
        (IsAutoRotation = 0 AND RotationScheduleDays IS NULL) OR
        (IsAutoRotation = 1 AND RotationScheduleDays > 0)
    )
);

CREATE INDEX IX_EncryptionKeys_ClientId_Status ON EncryptionKeys(ClientId, KeyStatus);
CREATE INDEX IX_EncryptionKeys_ExpiresAt ON EncryptionKeys(ExpiresAt);
CREATE INDEX IX_EncryptionKeys_KeyVersion ON EncryptionKeys(ClientId, KeyVersion);
CREATE INDEX IX_EncryptionKeys_IsAutoRotation ON EncryptionKeys(IsAutoRotation, ExpiresAt);
GO

