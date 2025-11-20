namespace SECUiDEA_KMS.Models;

/// <summary>
/// appsettings 모델에서 암호화가 필요한 속성에 지정하는 특성
/// </summary>
/// <remarks>
/// <para>⚠️ 이 특성은 <see cref="string"/> 타입 속성에만 사용해야 합니다.</para>
/// <para>다른 타입(int, bool 등)에 사용하면 런타임에 <see cref="InvalidOperationException"/>이 발생합니다.</para>
/// <example>
/// 올바른 사용:
/// <code>
/// [EncryptedSetting]
/// public string Password { get; set; }
/// </code>
/// 잘못된 사용:
/// <code>
/// [EncryptedSetting]
/// public int Port { get; set; }  // ❌ 오류 발생!
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EncryptedSettingAttribute : Attribute
{
}
