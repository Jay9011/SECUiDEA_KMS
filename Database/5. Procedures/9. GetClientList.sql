IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'GetClientList')
BEGIN
    DROP PROCEDURE GetClientList;
END
GO
-- =============================================
-- 클라이언트 리스트를 페이징하여 조회
-- 최근 접근 순으로 정렬
-- =============================================
CREATE PROCEDURE GetClientList
    @PageNumber INT = 1,
    @PageSize   INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @TotalCount INT;
    DECLARE @Offset INT;
    
    BEGIN TRY
        -- 유효성 검증
        IF @PageNumber < 1 SET @PageNumber = 1;
        IF @PageSize < 1 OR @PageSize > 100 SET @PageSize = 10;
        
        SET @Offset = (@PageNumber - 1) * @PageSize;
        
        -- 전체 개수 조회
        SELECT @TotalCount = COUNT(*) FROM ClientServers;
        
        SELECT @TotalCount AS TotalCount;

        -- 페이징된 클라이언트 리스트 조회
        SELECT 
            ClientId,
            ClientGuid,
            ClientName,
            ClientIP,
            IPValidationMode,
            Description,
            IsActive,
            CreatedAt,
            CreatedBy,
            ModifiedAt,
            ModifiedBy,
            LastAccessAt,
            LastAccessIP
        FROM ClientServers
        ORDER BY 
            CASE WHEN LastAccessAt IS NULL THEN 1 ELSE 0 END,
            LastAccessAt DESC,
            CreatedAt DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        
    END TRY
    BEGIN CATCH
        SET @ErrorCode = '9999';
        SET @ErrorMessage = ERROR_MESSAGE();
        
        -- 에러 로그
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, ActionResult, Details)
        VALUES ('GetClientListFailed', 'Error', 'Client', 'Failed', @ErrorMessage);
    END CATCH
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage;
END
GO

