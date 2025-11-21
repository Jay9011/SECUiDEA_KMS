IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'CleanupExpiredKeys')
BEGIN
    DROP PROCEDURE CleanupExpiredKeys;
END
GO
-- =============================================
-- 만료된 키를 정리하는 유지보수 프로시저
-- 스케줄러를 통해 주기적으로 실행
-- =============================================
CREATE PROCEDURE CleanupExpiredKeys
    @RetentionDays INT = 90 -- 만료 후 보관 기간
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @ExpiredCount INT = 0;
    DECLARE @DeletedCount INT = 0;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 만료된 키를 Expired 상태로 변경
        UPDATE  EncryptionKeys
        SET     KeyStatus = 'Expired'
        WHERE   KeyStatus = 'Active' 
          AND   ExpiresAt < GETDATE();
        
        SET @ExpiredCount = @@ROWCOUNT;
        
        -- 보관 기간이 지난 Expired 키 삭제
        DECLARE @DeleteBeforeDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETDATE());
        
        DELETE FROM EncryptionKeys
        WHERE KeyStatus = 'Expired' 
          AND ExpiresAt < @DeleteBeforeDate;
        
        SET @DeletedCount = @@ROWCOUNT;

        -- 결과 반환
        SELECT  @ExpiredCount AS ExpiredKeys,
                @DeletedCount AS DeletedKeys;
        
        -- 감사 로그
        DECLARE @CleanupJson NVARCHAR(MAX) = 
            '{"ExpiredCount":' + CAST(@ExpiredCount AS NVARCHAR(10)) + 
            ',"DeletedCount":' + CAST(@DeletedCount AS NVARCHAR(10)) + 
            ',"RetentionDays":' + CAST(@RetentionDays AS NVARCHAR(10)) + '}';
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActionResult, Details)
        VALUES ('KeyCleanup', 'Info', 'System', 'System', 'Success', @CleanupJson);
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SET @ErrorCode = '9999';
        SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END
GO

