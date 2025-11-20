using CoreDAL.Configuration.Models;

namespace SECUiDEA_KMS.Models;

public class MsSqlDbSettings : MsSqlConnectionInfo
{
    [EncryptedSetting]
    override public string UserId { get; set; } = string.Empty;

    [EncryptedSetting]
    override public string Password { get; set; } = string.Empty;
}
