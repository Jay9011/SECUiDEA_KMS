using CoreDAL.ORM.Extensions;
using Microsoft.Extensions.Options;
using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.EncryptionKeys;
using SECUiDEA_KMS.Models.KeyRequests;
using SECUiDEA_KMS.Models.Settings;
using SECUiDEA_KMS.Repositories.Abstractions;

namespace SECUiDEA_KMS.Repositories;

/// <summary>
/// 키 관리 Repository 구현체
/// 실제 DB 호출 로직은 사용자가 직접 구현
/// </summary>
public class KeyRepository : BaseRepository, IKeyRepository
{
    #region 생성자

    public KeyRepository(IOptionsMonitor<MsSqlDbSettings> msSqlDbSettings) : base(msSqlDbSettings)
    {
    }

    #endregion

    public async Task<KmsResponse<EncryptionKeyEntity>> GenerateKeyAsync(GenerateKey_Proc proc, RequestInfo requestInfo)
    {
        try
        {
            proc.RequestIP = requestInfo.RequestIP;
            proc.RequestUserAgent = requestInfo.RequestUserAgent;
            proc.RequestHost = requestInfo.RequestHost;
            var result = await ExecuteProcedureAsync(Procs.GenerateKey, proc);
            var resultEntity = result.DataSet.Tables[result.DataSet.Tables.Count - 1].Rows[0].ToObject<ResultEntity>();
            if (resultEntity.IsSuccess)
            {
                var key = result.DataSet.Tables[0].Rows[0].ToObject<EncryptionKeyEntity>();
                return new KmsResponse<EncryptionKeyEntity>
                {
                    ErrorCode = resultEntity.ErrorCode,
                    ErrorMessage = resultEntity.ErrorMessage,
                    Data = key
                };
            }
            return new KmsResponse<EncryptionKeyEntity>
            {
                ErrorCode = resultEntity.ErrorCode,
                ErrorMessage = resultEntity.ErrorMessage,
                Data = null
            };
        }
        catch (Exception e)
        {
            return new KmsResponse<EncryptionKeyEntity>
            {
                ErrorCode = "9999",
                ErrorMessage = $"Repository Exception: 키 생성 중 오류 발생: {e.Message}",
                Data = null
            };
        }
    }

    public async Task<KmsResponse<EncryptionKeyEntity>> GetKeyAsync(GetKey_Proc proc, RequestInfo requestInfo)
    {
        try
        {
            proc.RequestIP = requestInfo.RequestIP;
            proc.RequestUserAgent = requestInfo.RequestUserAgent;
            proc.RequestHost = requestInfo.RequestHost;
            proc.RequestPath = requestInfo.RequestPath;
            var result = await ExecuteProcedureAsync(Procs.GetKey, proc);
            var resultEntity = result.DataSet.Tables[result.DataSet.Tables.Count - 1].Rows[0].ToObject<ResultEntity>();
            if (resultEntity.IsSuccess)
            {
                var key = result.DataSet.Tables[0].Rows[0].ToObject<EncryptionKeyEntity>();
                return new KmsResponse<EncryptionKeyEntity>
                {
                    ErrorCode = resultEntity.ErrorCode,
                    ErrorMessage = resultEntity.ErrorMessage,
                    Data = key
                };
            }
            return new KmsResponse<EncryptionKeyEntity>
            {
                ErrorCode = resultEntity.ErrorCode,
                ErrorMessage = resultEntity.ErrorMessage,
                Data = null
            };
        }
        catch (Exception e)
        {
            return new KmsResponse<EncryptionKeyEntity>
            {
                ErrorCode = "9999",
                ErrorMessage = $"Repository Exception: 키 조회 중 오류 발생: {e.Message}",
                Data = null
            };
        }
    }
}

