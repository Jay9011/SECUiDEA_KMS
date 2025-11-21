using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.ClientServers;

namespace SECUiDEA_KMS.Repositories;

/// <summary>
/// 클라이언트 관리 Repository 인터페이스
/// </summary>
public interface IClientRepository
{
    /// <summary>
    /// 클라이언트 리스트를 페이징하여 조회
    /// </summary>
    /// <param name="pageModel">페이지 모델</param>
    /// <returns>클라이언트 리스트 응답</returns>
    Task<KmsResponse<ClientListDTO>> GetClientListAsync(PageModel pageModel);

    /// <summary>
    /// 새 클라이언트 등록
    /// </summary>
    /// <param name="clientServer">클라이언트 등록 요청</param>
    /// <returns>등록된 클라이언트 정보</returns>
    Task<KmsResponse<ClientServerEntity>> RegisterClientAsync(ClientServerEntity clientServer);

    /// <summary>
    /// 클라이언트 상세 정보 조회
    /// </summary>
    /// <param name="clientServer">클라이언트 상세 정보 요청</param>
    /// <returns>클라이언트 상세 정보</returns>
    Task<KmsResponse<ClientServerEntity>> GetClientInfoAsync(ClientServerEntity clientServer);
}

