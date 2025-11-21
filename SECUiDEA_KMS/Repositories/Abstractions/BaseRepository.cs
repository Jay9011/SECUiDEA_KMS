using CoreDAL.Configuration;
using CoreDAL.DALs.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Interfaces;
using Microsoft.Extensions.Options;
using SECUiDEA_KMS.Models.Settings;

namespace SECUiDEA_KMS.Repositories.Abstractions;

public class BaseRepository
{
    protected readonly ICoreDAL _coreDAL;
    protected readonly IOptionsMonitor<MsSqlDbSettings> _dbSettings;
    protected readonly string _optionsName;

    protected BaseRepository(IOptionsMonitor<MsSqlDbSettings> msSqlDbSettings, string optionsName = Consts.SECUiDEA)
    {
        _dbSettings = msSqlDbSettings;
        _optionsName = optionsName;
        _coreDAL = DbDalFactory.CreateCoreDal(DatabaseType.MSSQL);
    }

    /// <summary>
    /// 저장 프로시저 실행 (ORM 스타일)
    /// </summary>
    /// <param name="procedureName">저장 프로시저 이름</param>
    /// <param name="parameters">저장 프로시저 파라미터</param>
    /// <param name="isReturn">반환 값 여부</param>
    /// <returns><see cref="SQLResult"/></returns>
    protected async Task<SQLResult> ExecuteProcedureAsync(string procedureName, ISQLParam? parameters = null, bool isReturn = true)
    {
        var connectionString = _dbSettings.Get(_optionsName).ToConnectionString();
        return await _coreDAL.ExecuteProcedureAsync(connectionString, procedureName, parameters, isReturn);
    }

    /// <summary>
    /// 저장 프로시저 실행 (Dictionary 스타일)
    /// </summary>
    /// <param name="procedureName">저장 프로시저 이름</param>
    /// <param name="parameters">저장 프로시저 파라미터</param>
    /// <param name="isReturn">반환 값 여부</param>
    /// <returns><see cref="SQLResult"/></returns>
    protected async Task<SQLResult> ExecuteProcedureAsync(string procedureName, Dictionary<string, object>? parameters = null, bool isReturn = true)
    {
        var connectionString = _dbSettings.Get(_optionsName).ToConnectionString();
        return await _coreDAL.ExecuteProcedureAsync(connectionString, procedureName, parameters, isReturn);
    }
}
