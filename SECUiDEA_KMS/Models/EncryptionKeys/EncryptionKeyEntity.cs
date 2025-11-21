using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace SECUiDEA_KMS.Models.EncryptionKeys;

/// <summary>
/// 암호화 키 정보 Entity
/// </summary>
public class EncryptionKeyEntity : SQLParam
{
    public int KeyId { get; set; }
    [DbParameter]
    public int? ClientId { get; set; }
    [DbParameter]
    public byte[] EncryptedKeyData { get; set; } = Array.Empty<byte>();
    public int KeyVersion { get; set; }
    public string KeyStatus { get; set; } = "Active";
    [DbParameter]
    public bool? IsAutoRotation { get; set; }
    [DbParameter]
    public int? RotationScheduleDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
}

