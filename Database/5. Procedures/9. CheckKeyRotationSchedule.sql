-- =============================================
-- 자동 회전이 필요한 키 확인
-- 스케줄러를 통해 주기적으로 실행하여 회전 알림
-- =============================================
CREATE PROCEDURE CheckKeyRotationSchedule
    @DaysBeforeExpiration INT = 7 -- 만료 며칠 전에 알림
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @Count INT = 0;
    
    BEGIN TRY
        -- 자동 회전이 설정되어 있고, 만료가 임박한 키 조회
        SELECT 
            CS.ClientId,
            CS.ClientGuid,
            CS.ClientName,
            EK.KeyId,
            EK.KeyVersion,
            EK.CreatedAt,
            EK.ExpiresAt,
            EK.RotationScheduleDays,
            DATEDIFF(DAY, GETDATE(), EK.ExpiresAt) AS DaysUntilExpiration
        FROM EncryptionKeys EK
        INNER JOIN ClientServers CS ON EK.ClientId = CS.ClientId
        WHERE EK.IsAutoRotation = 1
            AND EK.KeyStatus = 'Active'
            AND EK.ExpiresAt <= DATEADD(DAY, @DaysBeforeExpiration, GETDATE())
        ORDER BY EK.ExpiresAt ASC;
        
        -- 개수 반환
        SET @Count = @@ROWCOUNT;
        SELECT  @Count AS KeysRequiringRotation;
        
        -- 감사 로그
        IF @Count > 0
        BEGIN
            DECLARE @RotationCheckJson NVARCHAR(MAX) = 
                '{"KeysRequiringRotation":' + CAST(@Count AS NVARCHAR(10)) + 
                ',"DaysBeforeExpiration":' + CAST(@DaysBeforeExpiration AS NVARCHAR(10)) + '}';
            
            INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActionResult, Details)
            VALUES ('KeyRotationRequired', 'Warning', 'System', 'System', 'Success', @RotationCheckJson);
        END
        
    END TRY
    BEGIN CATCH
        SET @ErrorCode = '9999';
        SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END
GO

