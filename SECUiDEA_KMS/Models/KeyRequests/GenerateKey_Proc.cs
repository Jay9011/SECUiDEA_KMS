using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace SECUiDEA_KMS.Models.KeyRequests;

public class GenerateKey_Proc : SQLParam
{
    [DbParameter]
    public Guid ClientGuid { get; set; }
    [DbParameter]
    public byte[] EncryptedKeyData { get; set; } = Array.Empty<byte>();
    [DbParameter]
    public bool IsAutoRotation { get; set; } = false;
    [DbParameter]
    public int? ExpirationDays { get; set; }
    [DbParameter]
    public int? RotationScheduleDays { get; set; }
    [DbParameter]
    public string? RequestIP { get; set; }
    [DbParameter]
    public string? RequestUserAgent { get; set; }
    [DbParameter]
    public string? RequestHost { get; set; }
    [DbParameter]
    public bool SkipIpValidation { get; set; } = false;
}
