IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'GetKeyUsageStats')
BEGIN
    DROP PROCEDURE GetKeyUsageStats;
END
GO
-- =============================================
-- 클라이언트별 키 사용 통계 조회
-- =============================================
CREATE PROCEDURE GetKeyUsageStats
    @ClientGuid UNIQUEIDENTIFIER = NULL,
    @StartDate  DATETIME2 = NULL,
    @EndDate    DATETIME2 = NULL,
    @TopN       INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @ClientId INT = NULL;
    
    BEGIN TRY
        -- 기본값 설정
        IF @StartDate IS NULL SET @StartDate = DATEADD(DAY, -30, GETDATE());
        IF @EndDate IS NULL SET @EndDate = GETDATE();
        
        -- ClientGuid가 제공된 경우 ClientId 조회
        IF (@ClientGuid IS NOT NULL)
        BEGIN
            SELECT  @ClientId = ClientId 
            FROM    ClientServers 
            WHERE   ClientGuid = @ClientGuid;
            
            IF (@ClientId IS NULL)
            BEGIN
                SET @ErrorCode = '1001';
                SET @ErrorMessage = 'Client not found';
            END
        END
        
        -- 통계 조회
        SELECT TOP (@TopN)
            CS.ClientName,
            CS.ClientGuid,
            KR.Operation,
            COUNT(*) AS TotalRequests,
            SUM(CASE WHEN KR.Success = 1 THEN 1 ELSE 0 END) AS SuccessCount,
            SUM(CASE WHEN KR.Success = 0 THEN 1 ELSE 0 END) AS FailureCount,
            CAST(AVG(CAST(KR.Success AS FLOAT)) * 100 AS DECIMAL(5,2)) AS SuccessRate,
            AVG(KR.ResponseTimeMs) AS AvgResponseTimeMs,
            MAX(KR.ResponseTimeMs) AS MaxResponseTimeMs,
            MIN(KR.RequestTimestamp) AS FirstRequest,
            MAX(KR.RequestTimestamp) AS LastRequest
        FROM KeyRequests KR
        INNER JOIN ClientServers CS ON KR.ClientId = CS.ClientId
        WHERE (@ClientId IS NULL OR KR.ClientId = @ClientId)
            AND KR.RequestTimestamp BETWEEN @StartDate AND @EndDate
        GROUP BY CS.ClientName, CS.ClientGuid, KR.Operation
        ORDER BY TotalRequests DESC;
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

