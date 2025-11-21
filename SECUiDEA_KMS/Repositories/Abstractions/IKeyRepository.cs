using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.EncryptionKeys;
using SECUiDEA_KMS.Models.KeyRequests;

namespace SECUiDEA_KMS.Repositories;

/// <summary>
/// 키 관리 Repository 인터페이스
/// </summary>
public interface IKeyRepository
{
    /// <summary>
    /// 새 암호화 키 생성
    /// </summary>
    /// <param name="proc">키 생성 프로시저</param>
    /// <param name="requestInfo">요청 정보</param>
    /// <returns>생성된 키 정보</returns>
    Task<KmsResponse<EncryptionKeyEntity>> GenerateKeyAsync(GenerateKey_Proc proc, RequestInfo requestInfo);

    /// <summary>
    /// 클라이언트의 활성 키 조회
    /// </summary>
    /// <param name="proc">키 조회 프로시저</param>
    /// <param name="requestInfo">요청 정보</param>
    /// <returns>활성 키 정보</returns>
    Task<KmsResponse<EncryptionKeyEntity>> GetKeyAsync(GetKey_Proc proc, RequestInfo requestInfo);
}

