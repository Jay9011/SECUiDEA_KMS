IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'GetClientInfo')
BEGIN
    DROP PROCEDURE GetClientInfo;
END
GO
-- =============================================
-- 클라이언트 정보 조회 (GUID 기반)
-- =============================================
CREATE PROCEDURE GetClientInfo
    @ClientGuid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    
    BEGIN TRY
        SELECT 
            CS.ClientId,
            CS.ClientGuid,
            CS.ClientName,
            CS.ClientIP,
            CS.Description,
            CS.IsActive,
            CS.IPValidationMode,
            CS.CreatedAt,
            CS.CreatedBy,
            CS.ModifiedAt,
            CS.ModifiedBy,
            CS.LastAccessAt,
            CS.LastAccessIP,
            -- 활성 키 정보
            (SELECT COUNT(*) FROM EncryptionKeys WHERE ClientId = CS.ClientId AND KeyStatus = 'Active') AS ActiveKeyCount,
            (SELECT MAX(KeyVersion) FROM EncryptionKeys WHERE ClientId = CS.ClientId) AS CurrentKeyVersion,
            (SELECT TOP 1 ExpiresAt FROM EncryptionKeys WHERE ClientId = CS.ClientId AND KeyStatus = 'Active' ORDER BY KeyVersion DESC) AS CurrentKeyExpiresAt
        FROM ClientServers CS
        WHERE CS.ClientGuid = @ClientGuid;
        
        IF @@ROWCOUNT = 0
        BEGIN
            SET @ErrorCode = '1001';
            SET @ErrorMessage = 'Client not found';
        END
        
    END TRY
    BEGIN CATCH
        SET @ErrorCode = '9999';
        SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage;
END;
GO

