IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'RotateKey')
BEGIN
    DROP PROCEDURE RotateKey;
END
GO
-- =============================================
-- 기존 키를 새 키로 교체 (키 회전)
-- 이전 키는 일정 기간 보관하여 이전 데이터 복호화 지원
-- =============================================
CREATE PROCEDURE RotateKey
    @ClientGuid             UNIQUEIDENTIFIER,
    @NewEncryptedKeyData    VARBINARY(MAX),
    @RotationType           NVARCHAR(50)    = 'Manual', -- Manual, Scheduled, Forced
    @RotatedBy              NVARCHAR(100),
    @Reason                 NVARCHAR(500)   = NULL,
    @IsAutoRotation         BIT             = 0, -- 새 키의 자동 회전 여부
    @ExpirationDays         INT             = NULL,
    @RotationScheduleDays   INT             = NULL,
    @RequestIP              NVARCHAR(50)    = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @OldKeyId BIGINT;
    DECLARE @NewKeyId BIGINT;
    DECLARE @StartTime DATETIME2 = GETDATE();
    DECLARE @ClientId INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- ClientGuid로 ClientId 조회
        SELECT  @ClientId = ClientId 
        FROM    ClientServers 
        WHERE   ClientGuid = @ClientGuid 
          AND   IsActive = 1;
        
        IF (@ClientId IS NULL)
        BEGIN
            SET @ErrorCode = '1001';
            SET @ErrorMessage = 'Client not found or inactive';
            THROW 50001, @ErrorMessage, 1;
        END
        
        -- 현재 활성 키 확인
        SELECT TOP 1 @OldKeyId = KeyId
        FROM    EncryptionKeys
        WHERE   ClientId = @ClientId 
          AND   KeyStatus = 'Active'
        ORDER BY KeyVersion DESC;
        
        IF (@OldKeyId IS NULL)
        BEGIN
            SET @ErrorCode = '2004';
            SET @ErrorMessage = 'No active key found to rotate';
            THROW 50004, @ErrorMessage, 1;
        END
        
        -- 자동 회전 설정 검증
        IF (@IsAutoRotation = 1)
        BEGIN
            IF (@ExpirationDays IS NULL OR @ExpirationDays <= 0)
            BEGIN
                SET @ErrorCode = '2002';
                SET @ErrorMessage = 'ExpirationDays is required when IsAutoRotation is true';
                THROW 50003, @ErrorMessage, 1;
            END
            IF (@RotationScheduleDays IS NULL OR @RotationScheduleDays <= 0)
            BEGIN
                SET @ErrorCode = '2003';
                SET @ErrorMessage = 'RotationScheduleDays is required when IsAutoRotation is true';
                THROW 50005, @ErrorMessage, 1;
            END
        END
        
        -- 기존 키를 Expired로 변경
        UPDATE  EncryptionKeys 
        SET     KeyStatus = 'Expired'
        WHERE   KeyId = @OldKeyId;
        
        -- 새 키 생성
        DECLARE @ExpiresAt DATETIME2;
        DECLARE @NewVersion INT;
        
        IF (@IsAutoRotation = 0)
        BEGIN
            SET @ExpiresAt = DATEADD(YEAR, 10, GETDATE());
        END
        ELSE
        BEGIN
            SET @ExpiresAt = DATEADD(DAY, @ExpirationDays, GETDATE());
        END
        
        SELECT @NewVersion = MAX(KeyVersion) + 1
        FROM EncryptionKeys
        WHERE ClientId = @ClientId;
        
        INSERT INTO EncryptionKeys (ClientId, EncryptedKeyData, KeyVersion, IsAutoRotation, 
                                     RotationScheduleDays, ExpiresAt)
        VALUES (@ClientId, @NewEncryptedKeyData, @NewVersion, @IsAutoRotation, 
                @RotationScheduleDays, @ExpiresAt);
        
        SET @NewKeyId = SCOPE_IDENTITY();

        -- 새 키 정보 반환
        SELECT  @NewKeyId AS NewKeyId,
                @OldKeyId AS OldKeyId
        ;
        -- 회전 이력 기록
        INSERT INTO KeyRotationHistory (ClientId, OldKeyId, NewKeyId, RotationType, RotatedBy, Reason)
        VALUES (@ClientId, @OldKeyId, @NewKeyId, @RotationType, @RotatedBy, @Reason);
        
        -- 감사 로그
        DECLARE @RotateJson NVARCHAR(MAX) = 
            '{"OldKeyId":' + CAST(@OldKeyId AS NVARCHAR(20)) + 
            ',"NewKeyId":' + CAST(@NewKeyId AS NVARCHAR(20)) + 
            ',"RotationType":"' + @RotationType + 
            '","Reason":"' + ISNULL(REPLACE(@Reason, '"', '\"'), '') + 
            '","IsAutoRotation":' + CAST(@IsAutoRotation AS NVARCHAR(1)) + '}';
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, ResourceId, Actor, ActorIP, ActionResult, Details)
        VALUES ('KeyRotated', 'Info', 'Key', CAST(@NewKeyId AS NVARCHAR(50)), 
                @RotatedBy, @RequestIP, 'Success', @RotateJson);
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        IF @ErrorCode = '0000'
        BEGIN
            SET @ErrorCode = '9999';
            SET @ErrorMessage = ERROR_MESSAGE();
        END
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActorIP, ActionResult, Details)
        VALUES ('KeyRotationFailed', 'Error', 'Key', 
                CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Failed', @ErrorMessage);
    END CATCH
    
    -- 요청 로그 기록
    DECLARE @ResponseTime INT = DATEDIFF(MILLISECOND, @StartTime, GETDATE());
    
    INSERT INTO KeyRequests (ClientId, KeyId, Operation, RequestIP, Success, ResponseTimeMs)
    VALUES (@ClientId, @NewKeyId, 'RotateKey', @RequestIP, 
            CASE WHEN @ErrorCode = '0000' THEN 1 ELSE 0 END, @ResponseTime);
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END
GO

