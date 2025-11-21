IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'CheckRateLimit')
BEGIN
    DROP PROCEDURE CheckRateLimit;
END
GO
-- =============================================
-- Rate Limit 체크 및 기록 (내부 사용)
-- 설정된 시간 내 최대 요청 수를 초과하면 차단
-- =============================================
CREATE PROCEDURE CheckRateLimit
    @RequestIP NVARCHAR(50),
    @IsBlocked BIT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RateLimitSeconds INT;
    DECLARE @MaxRequests INT;
    DECLARE @IsEnabled BIT;
    DECLARE @CurrentCount INT = 0;
    DECLARE @WindowStart DATETIME2;
    
    -- Rate Limit 설정 조회
    SELECT TOP 1    @RateLimitSeconds = RateLimitSeconds,
                    @MaxRequests = MaxRequests,
                    @IsEnabled = IsEnabled
    FROM    RateLimitsSettings
    WHERE   IsEnabled = 1
    ORDER BY CreatedAt DESC;
    
    -- Rate Limit이 비활성화되어 있으면 통과
    IF (@IsEnabled IS NULL OR @IsEnabled = 0)
    BEGIN
        SET @IsBlocked = 0;
        RETURN;
    END
    
    -- 기본값 설정
    IF @RateLimitSeconds IS NULL SET @RateLimitSeconds = 60;
    IF @MaxRequests IS NULL SET @MaxRequests = 100;
    
    SET @WindowStart = DATEADD(SECOND, -@RateLimitSeconds, GETDATE());
    
    -- 이미 차단된 IP인지 확인
    IF EXISTS ( SELECT  1 
                FROM    RateLimits 
                WHERE   RequestIP = @RequestIP 
                  AND   IsBlocked = 1 
                  AND   BlockedUntil > GETDATE()
    )
    BEGIN
        SET @IsBlocked = 1;
        RETURN;
    END
    
    -- 현재 시간 윈도우 내의 요청 수 계산
    SELECT  @CurrentCount = COUNT(*)
    FROM    RateLimits
    WHERE   RequestIP = @RequestIP
      AND   RequestTimestamp >= @WindowStart;
    
    -- 요청 기록
    INSERT INTO RateLimits (RequestIP, RequestTimestamp, RequestCount)
    VALUES (@RequestIP, GETDATE(), @CurrentCount + 1);
    
    -- 제한 초과 시 차단
    IF @CurrentCount >= @MaxRequests
    BEGIN
        SET @IsBlocked = 1;
        
        -- 차단 정보 업데이트
        UPDATE  RateLimits
        SET     IsBlocked = 1,
                BlockedUntil = DATEADD(MINUTE, 15, GETDATE()) -- 15분 차단
        WHERE   RequestIP = @RequestIP
          AND   RequestTimestamp >= @WindowStart;
    END
    ELSE
    BEGIN
        SET @IsBlocked = 0;
    END
    
    -- 오래된 기록 정리 (7일 이전)
    DELETE FROM RateLimits
    WHERE RequestTimestamp < DATEADD(DAY, -7, GETDATE());
END;
GO

