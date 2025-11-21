IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'InitializeRateLimitSettings')
BEGIN
    DROP PROCEDURE InitializeRateLimitSettings;
END
GO
-- =============================================
-- Rate Limit 설정 초기화
-- 최초 1회만 실행하여 기본 설정 생성
-- =============================================
CREATE PROCEDURE InitializeRateLimitSettings
    @RateLimitSeconds INT = 60,
    @MaxRequests INT = 100,
    @CreatedBy NVARCHAR(100) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    
    BEGIN TRY
        -- 이미 설정이 있는지 확인
        IF NOT EXISTS (SELECT 1 FROM RateLimitsSettings)
        BEGIN
            INSERT INTO RateLimitsSettings (RateLimitSeconds, MaxRequests, IsEnabled, CreatedBy)
            VALUES (@RateLimitSeconds, @MaxRequests, 1, @CreatedBy);

            -- 결과 반환
            SELECT  @RateLimitSeconds AS RateLimitSeconds,
                    @MaxRequests AS MaxRequests;
            
            SET @ErrorMessage = 'Rate Limit Settings Initialized';
        END
        ELSE
        BEGIN
            SET @ErrorMessage = 'Rate Limit Settings Already Exist';
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

