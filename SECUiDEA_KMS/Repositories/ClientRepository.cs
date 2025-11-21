using CoreDAL.ORM.Extensions;
using Microsoft.Extensions.Options;
using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.ClientServers;
using SECUiDEA_KMS.Models.Settings;
using SECUiDEA_KMS.Repositories.Abstractions;

namespace SECUiDEA_KMS.Repositories;

/// <summary>
/// 클라이언트 관리 Repository 구현체
/// 실제 DB 호출 로직은 사용자가 직접 구현
/// </summary>
public class ClientRepository : BaseRepository, IClientRepository
{
    #region 생성자

    public ClientRepository(IOptionsMonitor<MsSqlDbSettings> msSqlDbSettings) : base(msSqlDbSettings)
    {
    }

    #endregion

    public async Task<KmsResponse<ClientListDTO>> GetClientListAsync(PageModel pageModel)
    {
        try
        {
            var result = await ExecuteProcedureAsync(Procs.GetClientList, pageModel);
            var resultEntity = result.DataSet.Tables[result.DataSet.Tables.Count - 1].Rows[0].ToObject<ResultEntity>();

            if (resultEntity.IsSuccess)
            {
                var totalCount = result.DataSet.Tables[0].Rows[0].ToObject<TotalCountEntity>();
                var clients = result.DataSet.Tables[1].ToObject<ClientServerEntity>().ToList();
                var clientListDTO = new ClientListDTO
                {
                    Clients = clients,
                    TotalCount = totalCount.TotalCount,
                    PageNumber = pageModel.PageNumber,
                    PageSize = pageModel.PageSize
                };

                return new KmsResponse<ClientListDTO>
                {
                    ErrorCode = resultEntity.ErrorCode,
                    ErrorMessage = resultEntity.ErrorMessage,
                    Data = clientListDTO
                };
            }
            return new KmsResponse<ClientListDTO>
            {
                ErrorCode = resultEntity.ErrorCode,
                ErrorMessage = resultEntity.ErrorMessage,
                Data = null
            };
        }
        catch (Exception e)
        {
            return new KmsResponse<ClientListDTO>
            {
                ErrorCode = "9999",
                ErrorMessage = $"Repository Exception: 클라이언트 리스트 조회 중 오류 발생: {e.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// 새 클라이언트 등록
    /// 
    /// 구현 가이드:
    /// 1. RegisterClient 프로시저 호출 (@ClientName, @ClientIP, @Description, @IPValidationMode, @CreatedBy)
    /// 2. 첫 번째 ResultSet: 등록된 클라이언트 전체 정보
    /// 3. 두 번째 ResultSet: ErrorCode, ErrorMessage
    /// 4. KmsResponse로 래핑하여 반환
    /// </summary>
    public async Task<KmsResponse<ClientServerEntity>> RegisterClientAsync(ClientServerEntity clientServer)
    {
        try
        {
            var result = await ExecuteProcedureAsync(Procs.RegisterClient, clientServer);
            var resultEntity = result.DataSet.Tables[result.DataSet.Tables.Count - 1].Rows[0].ToObject<ResultEntity>();

            if (resultEntity.IsSuccess)
            {
                var client = result.DataSet.Tables[0].Rows[0].ToObject<ClientServerEntity>();
                return new KmsResponse<ClientServerEntity>
                {
                    ErrorCode = resultEntity.ErrorCode,
                    ErrorMessage = resultEntity.ErrorMessage,
                    Data = client
                };
            }

            return new KmsResponse<ClientServerEntity>
            {
                ErrorCode = resultEntity.ErrorCode,
                ErrorMessage = resultEntity.ErrorMessage,
                Data = null
            };
        }
        catch (Exception e)
        {
            return new KmsResponse<ClientServerEntity>
            {
                ErrorCode = "9999",
                ErrorMessage = $"Repository Exception: 클라이언트 등록 중 오류 발생: {e.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// 클라이언트 상세 정보 조회
    /// 
    /// 구현 가이드:
    /// 1. GetClientInfo 프로시저 호출 (@ClientGuid)
    /// 2. 첫 번째 ResultSet: 클라이언트 상세 정보
    /// 3. 두 번째 ResultSet: ErrorCode, ErrorMessage
    /// 4. KmsResponse로 래핑하여 반환
    /// </summary>
    public async Task<KmsResponse<ClientServerEntity>> GetClientInfoAsync(ClientServerEntity clientServer)
    {
        try
        {
            var result = await ExecuteProcedureAsync(Procs.GetClientInfo, clientServer);
            var resultEntity = result.DataSet.Tables[result.DataSet.Tables.Count - 1].Rows[0].ToObject<ResultEntity>();

            if (resultEntity.IsSuccess)
            {
                var client = result.DataSet.Tables[0].Rows[0].ToObject<ClientServerEntity>();
                return new KmsResponse<ClientServerEntity>
                {
                    ErrorCode = resultEntity.ErrorCode,
                    ErrorMessage = resultEntity.ErrorMessage,
                    Data = client
                };
            }

            return new KmsResponse<ClientServerEntity>
            {
                ErrorCode = resultEntity.ErrorCode,
                ErrorMessage = resultEntity.ErrorMessage,
                Data = null
            };
        }
        catch (Exception e)
        {
            return new KmsResponse<ClientServerEntity>
            {
                ErrorCode = "9999",
                ErrorMessage = $"Repository Exception: 클라이언트 상세 정보 조회 중 오류 발생: {e.Message}",
                Data = null
            };
        }
    }
}

