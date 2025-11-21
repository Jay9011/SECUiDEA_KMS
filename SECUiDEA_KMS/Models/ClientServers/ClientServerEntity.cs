using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace SECUiDEA_KMS.Models;

/// <summary>
/// 클라이언트 서버 정보 Entity
/// </summary>
public class ClientServerEntity : SQLParam
{
    [DbParameter]
    public int? ClientId { get; set; }
    [DbParameter]
    public Guid? ClientGuid { get; set; }
    [DbParameter]
    public string ClientName { get; set; } = string.Empty;
    [DbParameter]
    public string ClientIP { get; set; } = string.Empty;
    [DbParameter]
    public string IPValidationMode { get; set; } = "Strict";
    [DbParameter]
    public string? Description { get; set; }
    [DbParameter]
    public bool? IsActive { get; set; }
    [DbParameter]
    public string? CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    [DbParameter]
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    [DbParameter]
    public string? LastAccessIP { get; set; }
    public DateTime? LastAccessAt { get; set; }
}

