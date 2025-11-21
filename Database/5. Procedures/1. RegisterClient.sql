-- =============================================
-- 새로운 클라이언트를 KMS에 등록 (관리자용)
-- GUID는 서버에서 자동 생성하여 반환
-- =============================================
CREATE PROCEDURE RegisterClient
    @ClientName         NVARCHAR(200),
    @ClientIP           NVARCHAR(50),
    @Description        NVARCHAR(500) = NULL,
    @IPValidationMode   NVARCHAR(50)  = 'Strict',
    @CreatedBy          NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorCode NVARCHAR(4) = '0000';
    DECLARE @ErrorMessage NVARCHAR(MAX) = 'Success';

    DECLARE @ClientId INT;
    DECLARE @ClientGuid UNIQUEIDENTIFIER;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        -- IP 중복 확인 (동일 IP로 이미 등록된 클라이언트가 있는지)
        IF EXISTS (SELECT 1 FROM ClientServers WHERE ClientIP = @ClientIP AND IsActive = 1)
        BEGIN
            SET @ErrorCode = '1003';
            SET @ErrorMessage = 'ClientIP already registered';
            THROW 50001, @ErrorMessage, 1;
        END
        
        -- 클라이언트 등록 (ClientGuid는 DEFAULT NEWID()로 자동 생성)
        INSERT INTO ClientServers (ClientName, ClientIP, Description, IPValidationMode, CreatedBy)
        VALUES (@ClientName, @ClientIP, @Description, @IPValidationMode, @CreatedBy);
        
        SET @ClientId = SCOPE_IDENTITY();
        
        -- 감사 로그
        DECLARE @JsonDetails NVARCHAR(MAX) = 
            '{"ClientGuid":"' + CAST(@ClientGuid AS NVARCHAR(50)) + 
            '","ClientName":"' + REPLACE(@ClientName, '"', '\"') + 
            '","ClientIP":"' + @ClientIP + '"}';
        
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, ResourceId, Actor, ActorIP, ActionResult, Details)
        VALUES ('ClientRegistered', 'Info', 'Client', CAST(@ClientId AS NVARCHAR(50)), 
                @CreatedBy, @ClientIP, 'Success', @JsonDetails);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        IF @ErrorCode = '0000'
        BEGIN
            SET @ErrorCode = '9999';
            SET @ErrorMessage = ERROR_MESSAGE();
        END
        
        -- 에러 로그
        INSERT INTO AuditLogs (EventType, Severity, ResourceType, Actor, ActorIP, ActionResult, Details)
        VALUES ('ClientRegistrationFailed', 'Error', 'Client', @CreatedBy, @ClientIP, 'Failed', @ErrorMessage);
    END CATCH
    
    -- 결과 반환
    SELECT 
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage
END;
GO

