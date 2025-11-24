using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace SECUiDEA_KMS.Models.KeyRequests;

public class GetKey_Proc : SQLParam
{
    [DbParameter]
    public string Type { get; set; } = "Active";
    [DbParameter]
    public Guid ClientGuid { get; set; }
    [DbParameter]
    public string? RequestIP { get; set; }
    [DbParameter]
    public string? RequestUserAgent { get; set; }
    [DbParameter]
    public string? RequestHost { get; set; }
    [DbParameter]
    public string? RequestPath { get; set; }
}
