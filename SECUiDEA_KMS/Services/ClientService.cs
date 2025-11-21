using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.ClientServers;
using SECUiDEA_KMS.Repositories;

namespace SECUiDEA_KMS.Services;

/// <summary>
/// 클라이언트 관리 서비스
/// 비즈니스 로직 처리 및 Repository 호출
/// </summary>
public class ClientService
{
    #region 의존 주입
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientService> _logger;

    public ClientService(IClientRepository clientRepository, ILogger<ClientService> logger)
    {
        _clientRepository = clientRepository;
        _logger = logger;
    }
    #endregion

    /// <summary>
    /// 클라이언트 리스트를 페이징하여 조회
    /// </summary>
    /// <param name="pageNumber">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>클라이언트 리스트 응답</returns>
    public async Task<KmsResponse<ClientListDTO>> GetClientListAsync(int pageNumber, int pageSize)
    {
        try
        {
            _logger.LogInformation("클라이언트 리스트 조회 시작: Page={PageNumber}, Size={PageSize}", pageNumber, pageSize);

            // 유효성 검증
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var response = await _clientRepository.GetClientListAsync(new PageModel() { PageNumber = pageNumber, PageSize = pageSize });

            if (response.IsSuccess)
            {
                _logger.LogInformation("클라이언트 리스트 조회 성공: TotalCount={TotalCount}", response.Data?.TotalCount ?? 0);
            }
            else
            {
                _logger.LogWarning("클라이언트 리스트 조회 실패: {ErrorCode} - {ErrorMessage}", response.ErrorCode, response.ErrorMessage);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "클라이언트 리스트 조회 중 예외 발생");
            return new KmsResponse<ClientListDTO>
            {
                ErrorCode = "9999",
                ErrorMessage = $"Service Exception: 클라이언트 리스트 조회 중 오류 발생: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// 새 클라이언트 등록
    /// </summary>
    public async Task<KmsResponse<ClientServerEntity>> RegisterClientAsync(RegisterClientDTO request, string createdBy)
    {
        try
        {
            _logger.LogInformation("클라이언트 등록 시작: Name={ClientName}, IP={ClientIP}", request.ClientName, request.ClientIP);

            var clientServer = new ClientServerEntity()
            {
                ClientName = request.ClientName,
                ClientIP = request.ClientIP,
                IPValidationMode = request.IPValidationMode,
                Description = request.Description,
                CreatedBy = createdBy
            };
            var response = await _clientRepository.RegisterClientAsync(clientServer);

            if (response.IsSuccess)
            {
                _logger.LogInformation("클라이언트 등록 성공: ClientId={ClientId}, ClientGuid={ClientGuid}",
                    response.Data?.ClientId, response.Data?.ClientGuid);
            }
            else
            {
                _logger.LogWarning("클라이언트 등록 실패: {ErrorCode} - {ErrorMessage}", response.ErrorCode, response.ErrorMessage);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "클라이언트 등록 중 예외 발생");
            return new KmsResponse<ClientServerEntity>
            {
                ErrorCode = "9999",
                ErrorMessage = $"클라이언트 등록 중 오류 발생: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// 클라이언트 상세 정보 조회
    /// </summary>
    public async Task<KmsResponse<ClientServerEntity>> GetClientInfoAsync(Guid clientGuid)
    {
        try
        {
            _logger.LogInformation("클라이언트 상세 조회 시작: ClientGuid={ClientGuid}", clientGuid);

            var clientServer = new ClientServerEntity()
            {
                ClientGuid = clientGuid
            };
            var response = await _clientRepository.GetClientInfoAsync(clientServer);

            if (response.IsSuccess)
            {
                _logger.LogInformation("클라이언트 상세 조회 성공: ClientId={ClientId}", response.Data?.ClientId);
            }
            else
            {
                _logger.LogWarning("클라이언트 상세 조회 실패: {ErrorCode} - {ErrorMessage}", response.ErrorCode, response.ErrorMessage);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "클라이언트 상세 조회 중 예외 발생");
            return new KmsResponse<ClientServerEntity>
            {
                ErrorCode = "9999",
                ErrorMessage = $"클라이언트 상세 조회 중 오류 발생: {ex.Message}",
                Data = null
            };
        }
    }
}

