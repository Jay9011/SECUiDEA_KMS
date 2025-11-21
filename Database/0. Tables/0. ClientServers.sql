-- =============================================
-- 클라이언트 서버 정보를 관리하는 테이블
-- 각 서버/애플리케이션을 GUID로 식별하고 접근 권한을 관리
-- =============================================
CREATE TABLE ClientServers (
    ClientId            INT                 PRIMARY KEY IDENTITY(1,1),
    ClientGuid          UNIQUEIDENTIFIER    UNIQUE NOT NULL DEFAULT NEWID(),
    ClientName          NVARCHAR(200)       NOT NULL,
    ClientIP            NVARCHAR(50)        NOT NULL, -- 클라이언트 등록 시 IP 주소
    IPValidationMode    NVARCHAR(50)        NOT NULL DEFAULT 'Strict', -- Strict, None
    Description         NVARCHAR(500),
    IsActive            BIT                 NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETDATE(),
    CreatedBy           NVARCHAR(100)       NOT NULL,
    ModifiedAt          DATETIME2,
    ModifiedBy          NVARCHAR(100),
    LastAccessAt        DATETIME2,
    LastAccessIP        NVARCHAR(50),
    CONSTRAINT CK_ClientName_NotEmpty CHECK (LEN(ClientName) > 0),
    CONSTRAINT CK_ClientIP_NotEmpty CHECK (LEN(ClientIP) > 0)
);

CREATE INDEX IX_ClientServers_ClientGuid ON ClientServers(ClientGuid);
CREATE INDEX IX_ClientServers_ClientIP ON ClientServers(ClientIP);
CREATE INDEX IX_ClientServers_IsActive ON ClientServers(IsActive);
CREATE INDEX IX_ClientServers_LastAccessAt ON ClientServers(LastAccessAt);
GO