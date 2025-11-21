DROP PROCEDURE IF EXISTS GetKey;
GO
-- =============================================
-- 클라이언트 GUID로 활성 암호화 키 조회
-- Rate Limit 적용 및 IP 검증
-- =============================================
CREATE PROCEDURE GetKey
    @ClientGuid         UNIQUEIDENTIFIER,
    @RequestIP          NVARCHAR(50)  = NULL,
    @RequestUserAgent   NVARCHAR(500) = NULL,
    @RequestHost        NVARCHAR(200) = NULL,
    @RequestPath        NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @StartTime DATETIME2 = GETDATE();
    DECLARE @Success BIT = 0;
    DECLARE @KeyId BIGINT = NULL;
    DECLARE @ClientId INT;
    DECLARE @IsBlocked BIT = 0;
    DECLARE @EncryptedKeyData VARBINARY(MAX);
    DECLARE @CreatedAt DATETIME2;
    DECLARE @ExpiresAt DATETIME2;
    
    BEGIN TRY
        -- Rate Limit 체크
        EXEC CheckRateLimit @RequestIP = @RequestIP, @IsBlocked = @IsBlocked OUTPUT;
        
        IF @IsBlocked = 1
        BEGIN
            SET @ErrorCode = '9999';
            SET @ErrorMessage = 'Rate limit exceeded';
            THROW 50010, @ErrorMessage, 1;
        END
        
        -- ClientGuid로 ClientId 조회 및 IP 검증
        DECLARE @RegisteredIP NVARCHAR(50);
        DECLARE @ValidationMode NVARCHAR(50);
        
        SELECT  @ClientId = ClientId,
                @RegisteredIP = ClientIP,
                @ValidationMode = IPValidationMode
        FROM    ClientServers 
        WHERE   ClientGuid = @ClientGuid 
          AND   IsActive = 1;
        
        IF (@ClientId IS NULL)
        BEGIN
            SET @ErrorCode = '1001';
            SET @ErrorMessage = 'Client not found or inactive';
            
            -- 보안 이벤트 로그
            INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActorIP, ActionResult, Details)
            VALUES ('UnauthorizedKeyAccess', 'Warning', 'Key', 
                    CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Denied', @ErrorMessage);
            
            THROW 50001, @ErrorMessage, 1;
        END
        
        -- IP 주소 검증 (Strict 모드인 경우)
        IF (@ValidationMode = 'Strict' AND @RequestIP IS NOT NULL AND @RegisteredIP != @RequestIP)
        BEGIN
            SET @ErrorCode = '1002';
            SET @ErrorMessage = 'IP address not allowed';
            
            -- 보안 이벤트 로그
            DECLARE @IPMismatchJson NVARCHAR(MAX) = 
                '{"RegisteredIP":"' + ISNULL(@RegisteredIP, '') + 
                '","RequestIP":"' + ISNULL(@RequestIP, '') + '"}';
            
            INSERT INTO AuditLogs (EventType, Severity, ResourceType, ResourceId, Actor, ActorIP, ActionResult, Details)
            VALUES ('IPMismatch', 'Warning', 'Key', CAST(@ClientId AS NVARCHAR(50)),
                    CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Denied', @IPMismatchJson);
            
            THROW 50002, @ErrorMessage, 1;
        END
        
        -- 활성 키 조회
        SELECT TOP 1    @KeyId = KeyId,
                        @EncryptedKeyData = EncryptedKeyData,
                        @CreatedAt = CreatedAt,
                        @ExpiresAt = ExpiresAt
        FROM    EncryptionKeys
        WHERE   ClientId = @ClientId 
          AND   KeyStatus = 'Active'
          AND   ExpiresAt > GETDATE()
        ORDER BY KeyVersion DESC;
        
        IF (@KeyId IS NULL)
        BEGIN
            SET @ErrorCode = '2001';
            SET @ErrorMessage = 'No active key found for this client';
            THROW 50003, @ErrorMessage, 1;
        END
        
        SET @Success = 1;
        
        -- 클라이언트 마지막 접근 정보 업데이트
        UPDATE  ClientServers 
        SET     LastAccessAt = GETDATE(), 
                LastAccessIP = @RequestIP
        WHERE   ClientId = @ClientId;
        
        -- 키 정보 반환
        SELECT  *
        FROM    EncryptionKeys
        WHERE   KeyId = @KeyId;

        -- 감사 로그
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, ResourceId, Actor, ActorIP, ActionResult)
        VALUES ('KeyAccessed', 'Info', 'Key', CAST(@KeyId AS NVARCHAR(50)), 
                CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Success');
        
    END TRY
    BEGIN CATCH
        IF @ErrorCode = '0000'
        BEGIN
            SET @ErrorCode = '9999';
            SET @ErrorMessage = ERROR_MESSAGE();
        END
        
        -- 에러 로그
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActorIP, ActionResult, Details)
        VALUES ('KeyAccessFailed', 'Warning', 'Key', 
                CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Failed', @ErrorMessage);
    END CATCH
    
    -- 요청 로그 기록
    DECLARE @ResponseTime INT = DATEDIFF(MILLISECOND, @StartTime, GETDATE());
    
    INSERT INTO KeyRequests (ClientId, KeyId, Operation, RequestIP, RequestUserAgent, 
                              RequestHost, RequestPath, Success, ErrorMessage, ResponseTimeMs)
    VALUES (@ClientId, @KeyId, 'GetKey', @RequestIP, @RequestUserAgent, 
            @RequestHost, @RequestPath, @Success, @ErrorMessage, @ResponseTime);
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END
GO

