namespace SECUiDEA_KMS;

public static class Procs
{
    public const string GetClientList = "GetClientList";
    public const string RegisterClient = "RegisterClient";
    public const string GetClientInfo = "GetClientInfo";
    public const string UpdateClientInfo = "UpdateClientInfo";
    public const string GenerateKey = "GenerateKey";
    public const string GetKey = "GetKey";
    public const string GetPreviousKey = "GetKey";
    public const string RotateKey = "RotateKey";
    public const string RevokeKey = "RevokeKey";
    public const string GetKeyUsageStats = "GetKeyUsageStats";
    public const string CleanupExpiredKeys = "CleanupExpiredKeys";
    public const string CheckKeyRotationSchedule = "CheckKeyRotationSchedule";
    public const string InitializeRateLimitSettings = "InitializeRateLimitSettings";
}
