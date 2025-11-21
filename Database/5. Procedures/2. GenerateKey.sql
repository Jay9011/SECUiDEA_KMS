DROP PROCEDURE IF EXISTS GenerateKey;
GO
-- =============================================
-- 클라이언트를 위한 새 암호화 키 생성
-- Rate Limit 적용 (IP 검증은 Service 레이어에서 수행)
-- =============================================
CREATE PROCEDURE GenerateKey
    @ClientGuid             UNIQUEIDENTIFIER,
    @EncryptedKeyData       VARBINARY(MAX), -- C# 레이어에서 암호화된 키
    @IsAutoRotation         BIT             = 0, -- 자동 회전 여부 (기본값: 반영구적)
    @ExpirationDays         INT             = NULL, -- IsAutoRotation이 1일 때만 필수
    @RotationScheduleDays   INT             = NULL, -- 자동 회전 주기
    @RequestIP              NVARCHAR(50)    = NULL,
    @RequestUserAgent       NVARCHAR(500)   = NULL,
    @RequestHost            NVARCHAR(200)   = NULL,
    @SkipIpValidation       BIT             = 0 -- IP 검증 스킵 여부 (관리자=1, 외부=0)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @StartTime DATETIME2 = GETDATE();
    DECLARE @Success BIT = 0;
    DECLARE @ClientId INT;
    DECLARE @IsBlocked BIT = 0;
    DECLARE @KeyId BIGINT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Rate Limit 체크
        EXEC CheckRateLimit @RequestIP = @RequestIP, @IsBlocked = @IsBlocked OUTPUT;
        
        IF @IsBlocked = 1
        BEGIN
            SET @ErrorCode = '9999';
            SET @ErrorMessage = 'Rate limit exceeded';
            THROW 50010, @ErrorMessage, 1;
        END
        
        -- ClientGuid로 ClientId 조회 (IP 검증은 Service 레이어에서 수행됨)
        SELECT  @ClientId = ClientId
        FROM    ClientServers 
        WHERE   ClientGuid = @ClientGuid 
          AND   IsActive = 1;
        
        IF @ClientId IS NULL
        BEGIN
            SET @ErrorCode = '1001';
            SET @ErrorMessage = 'Client not found or inactive';
            
            -- 보안 이벤트 로그
            INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActorIP, ActionResult, Details)
            VALUES ('UnauthorizedKeyGeneration', 'Warning', 'Key', 
                    CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Denied', @ErrorMessage);
            
            THROW 50001, @ErrorMessage, 1;
        END
        
        -- 자동 회전 설정 검증
        IF (@IsAutoRotation = 1)
        BEGIN
            IF @ExpirationDays IS NULL OR @ExpirationDays <= 0
            BEGIN
                SET @ErrorCode = '2002';
                SET @ErrorMessage = 'ExpirationDays is required when IsAutoRotation is true';
                THROW 50003, @ErrorMessage, 1;
            END
            IF @RotationScheduleDays IS NULL OR @RotationScheduleDays <= 0
            BEGIN
                SET @ErrorCode = '2003';
                SET @ErrorMessage = 'RotationScheduleDays is required when IsAutoRotation is true';
                THROW 50004, @ErrorMessage, 1;
            END
        END
        
        -- 반영구적 키인 경우 ExpiresAt을 10년 후로 설정
        DECLARE @ExpiresAt DATETIME2;
        IF (@IsAutoRotation = 0)
        BEGIN
            SET @ExpiresAt = DATEADD(YEAR, 10, GETDATE());
        END
        ELSE
        BEGIN
            SET @ExpiresAt = DATEADD(DAY, @ExpirationDays, GETDATE());
        END
        
        -- 기존 Active 키를 Expired로 변경
        UPDATE  EncryptionKeys 
        SET     KeyStatus = 'Expired'
        WHERE   ClientId = @ClientId 
          AND   KeyStatus = 'Active';
        
        -- 새 키 삽입
        INSERT INTO EncryptionKeys (ClientId, EncryptedKeyData, IsAutoRotation, RotationScheduleDays, ExpiresAt, KeyVersion)
        VALUES (@ClientId, @EncryptedKeyData, @IsAutoRotation, @RotationScheduleDays, @ExpiresAt,
                ISNULL((SELECT MAX(KeyVersion) FROM EncryptionKeys WHERE ClientId = @ClientId), 0) + 1);
        
        SET @KeyId = SCOPE_IDENTITY();
        SET @Success = 1;
        
        -- 클라이언트 마지막 접근 정보 업데이트
        UPDATE  ClientServers 
        SET     LastAccessAt = GETDATE(), LastAccessIP = @RequestIP
        WHERE   ClientId = @ClientId;

        -- 키 생성 정보 반환
        SELECT  *
        FROM    EncryptionKeys
        WHERE   KeyId = @KeyId;
        
        -- 감사 로그 (검증 스킵 여부 포함)
        DECLARE @ValidationSource NVARCHAR(20) = CASE WHEN @SkipIpValidation = 1 THEN 'Admin' ELSE 'External' END;
        DECLARE @KeyGenJson NVARCHAR(MAX) = 
            '{"KeyId":' + CAST(@KeyId AS NVARCHAR(20)) + 
            ',"ExpiresAt":"' + CONVERT(NVARCHAR(50), @ExpiresAt, 127) + 
            '","IsAutoRotation":' + CAST(@IsAutoRotation AS NVARCHAR(1)) + 
            ',"RotationScheduleDays":' + ISNULL(CAST(@RotationScheduleDays AS NVARCHAR(10)), 'null') + 
            ',"ValidationSource":"' + @ValidationSource + 
            '","SkipIpValidation":' + CAST(@SkipIpValidation AS NVARCHAR(1)) + '}';
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, ResourceId, Actor, ActorIP, ActionResult, Details)
        VALUES ('KeyGenerated', 'Info', 'Key', CAST(@KeyId AS NVARCHAR(50)), 
                CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Success', @KeyGenJson);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        IF @ErrorCode = '0000'
        BEGIN
            SET @ErrorCode = '9999';
            SET @ErrorMessage = ERROR_MESSAGE();
        END
        
        -- 에러 로그
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActorIP, ActionResult, Details)
        VALUES ('KeyGenerationFailed', 'Error', 'Key', 
                CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Failed', @ErrorMessage);
    END CATCH
    
    -- 요청 로그 기록
    DECLARE @ResponseTime INT = DATEDIFF(MILLISECOND, @StartTime, GETDATE());
    
    INSERT INTO KeyRequests (ClientId, KeyId, Operation, RequestIP, RequestUserAgent, 
                              RequestHost, Success, ErrorMessage, ResponseTimeMs)
    VALUES (@ClientId, @KeyId, 'GenerateKey', @RequestIP, @RequestUserAgent, 
            @RequestHost, @Success, @ErrorMessage, @ResponseTime);
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END
GO

