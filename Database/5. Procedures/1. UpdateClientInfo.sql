IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'UpdateClientInfo')
BEGIN
    DROP PROCEDURE UpdateClientInfo;
END
GO
-- =============================================
-- 클라이언트 정보 업데이트
-- =============================================
CREATE PROCEDURE UpdateClientInfo
    @ClientGuid         UNIQUEIDENTIFIER,
    @ClientName         NVARCHAR(200) = NULL,
    @ClientIP           NVARCHAR(50)  = NULL,
    @Description        NVARCHAR(500) = NULL,
    @IsActive           BIT           = NULL,
    @IPValidationMode   NVARCHAR(50)  = NULL,
    @ModifiedBy         NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';
    DECLARE @ClientId INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- ClientGuid로 ClientId 조회
        SELECT  @ClientId = ClientId 
        FROM    ClientServers 
        WHERE   ClientGuid = @ClientGuid;
        
        IF (@ClientId IS NULL)
        BEGIN
            SET @ErrorCode = '1001';
            SET @ErrorMessage = 'Client not found';
            THROW 50001, @ErrorMessage, 1;
        END
        
        -- 업데이트
        UPDATE ClientServers
        SET ClientName = ISNULL(@ClientName, ClientName),
            ClientIP = ISNULL(@ClientIP, ClientIP),
            Description = ISNULL(@Description, Description),
            IsActive = ISNULL(@IsActive, IsActive),
            IPValidationMode = ISNULL(@IPValidationMode, IPValidationMode),
            ModifiedAt = GETDATE(),
            ModifiedBy = @ModifiedBy
        WHERE ClientId = @ClientId;
        
        -- 감사 로그
        DECLARE @UpdateJson NVARCHAR(MAX) = 
            '{"ClientGuid":"' + CAST(@ClientGuid AS NVARCHAR(50)) + 
            '","IsActive":' + ISNULL(CAST(@IsActive AS NVARCHAR(1)), 'null') + '}';
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, ResourceId, Actor, ActionResult, Details)
        VALUES ('ClientInfoUpdated', 'Info', 'Client', CAST(@ClientId AS NVARCHAR(50)), 
                @ModifiedBy, 'Success', @UpdateJson);
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        IF @ErrorCode = '0000'
        BEGIN
            SET @ErrorCode = '9999';
            SET @ErrorMessage = ERROR_MESSAGE();
        END
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActionResult, Details)
        VALUES ('ClientInfoUpdateFailed', 'Error', 'Client', @ModifiedBy, 'Failed', @ErrorMessage);
    END CATCH
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END
GO

