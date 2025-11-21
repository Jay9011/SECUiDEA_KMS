-- =============================================
-- 키를 즉시 폐기 (보안 사고 발생 시)
-- =============================================
CREATE PROCEDURE RevokeKey
    @ClientGuid     UNIQUEIDENTIFIER,
    @Reason         NVARCHAR(500),
    @RevokedBy      NVARCHAR(100),
    @RequestIP      NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @ClientId INT;
    DECLARE @RevokedCount INT = 0;
    
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
        
        -- 모든 활성 키를 Revoked로 변경
        UPDATE EncryptionKeys 
        SET KeyStatus = 'Revoked',
            RevokedAt = GETDATE(),
            RevokedReason = @Reason
        WHERE ClientId = @ClientId 
          AND KeyStatus = 'Active';
        
        SET @RevokedCount = @@ROWCOUNT;
        
        IF @RevokedCount = 0
        BEGIN
            SET @ErrorCode = '2005';
            SET @ErrorMessage = 'No active keys to revoke';
            THROW 50005, @ErrorMessage, 1;
        END

        -- 폐기된 키 개수 반환
        SELECT  @RevokedCount AS RevokedKeyCount;
        
        -- 감사 로그 (Critical 수준)
        DECLARE @RevokeJson NVARCHAR(MAX) = 
            '{"RevokedCount":' + CAST(@RevokedCount AS NVARCHAR(10)) + 
            ',"Reason":"' + REPLACE(@Reason, '"', '\"') + '"}';
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, ResourceId, Actor, ActorIP, ActionResult, Details)
        VALUES ('KeyRevoked', 'Critical', 'Key', CAST(@ClientId AS NVARCHAR(50)), 
                @RevokedBy, @RequestIP, 'Success', @RevokeJson);
        
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
        VALUES ('KeyRevocationFailed', 'Error', 'Key', 
                CAST(@ClientGuid AS NVARCHAR(50)), @RequestIP, 'Failed', @ErrorMessage);
    END CATCH
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END
GO

