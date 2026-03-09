# SECUiDEA KMS 에러 코드 정의

## 📋 에러 코드 체계

### 0000 - Success
- **의미**: 요청이 성공적으로 처리됨
- **반환**: 모든 프로시저에서 성공 시 반환

---

## 1xxx - 클라이언트 관련 에러

### 1001 - Client Not Found or Inactive
- **의미**: 클라이언트를 찾을 수 없거나 비활성 상태
- **원인**: 
  - ClientGuid가 데이터베이스에 없음
  - IsActive = 0 (비활성화된 클라이언트)
- **대응**: 관리자에게 클라이언트 등록 또는 활성화 요청

### 1002 - IP Address Not Allowed
- **의미**: 요청한 IP 주소가 허용되지 않음
- **원인**: 
  - IPValidationMode = 'Strict'
  - RegisteredIP와 RequestIP가 다름
- **대응**: 
  - 관리자에게 IP 주소 업데이트 요청
  - IPValidationMode를 'None'으로 변경 (개발 환경)

### 1003 - ClientIP Already Registered
- **의미**: 동일한 IP로 이미 등록된 클라이언트가 있음
- **원인**: 중복 IP 등록 시도
- **대응**: 
  - 기존 클라이언트 확인
  - 필요시 기존 클라이언트 비활성화 후 재등록

---

## 2xxx - 키 관련 에러

### 2001 - No Active Key Found
- **의미**: 활성 암호화 키를 찾을 수 없음
- **원인**: 
  - 키가 생성되지 않음
  - 모든 키가 만료됨
  - 키가 폐기됨
- **대응**: GenerateKey 프로시저로 새 키 생성

### 2002 - ExpirationDays Required
- **의미**: IsAutoRotation = 1일 때 ExpirationDays가 필요함
- **원인**: 자동 회전 설정 시 필수 파라미터 누락
- **대응**: ExpirationDays 파라미터 전달 (양수 필요)

### 2003 - RotationScheduleDays Required
- **의미**: IsAutoRotation = 1일 때 RotationScheduleDays가 필요함
- **원인**: 자동 회전 설정 시 필수 파라미터 누락
- **대응**: RotationScheduleDays 파라미터 전달 (양수 필요)

### 2004 - No Active Key To Rotate
- **의미**: 회전할 활성 키가 없음
- **원인**: 현재 활성 상태인 키가 없음
- **대응**: GenerateKey로 먼저 키 생성

### 2005 - No Active Keys To Revoke
- **의미**: 폐기할 활성 키가 없음
- **원인**: 이미 모든 키가 폐기되었거나 만료됨
- **대응**: 이미 처리된 상태이므로 추가 작업 불필요

---

## 9xxx - 시스템 에러

### 9999 - Rate Limit Exceeded / General Error
- **의미**: 
  - 요청 제한 초과 (Rate Limit)
  - 예상치 못한 시스템 에러
- **원인**: 
  - 너무 많은 요청
  - SQL 실행 중 예외 발생
- **대응**: 
  - 잠시 후 재시도 (Rate Limit의 경우)
  - ErrorMessage 확인하여 상세 원인 파악

---

## 🔧 프로시저별 반환 가능한 에러 코드

### RegisterClient
- `0000`: Success
- `1003`: ClientIP Already Registered
- `9999`: General Error

### GenerateKey
- `0000`: Success
- `1001`: Client Not Found or Inactive
- `1002`: IP Address Not Allowed
- `2002`: ExpirationDays Required
- `2003`: RotationScheduleDays Required
- `9999`: Rate Limit Exceeded / General Error

### GetKey
- `0000`: Success
- `1001`: Client Not Found or Inactive
- `1002`: IP Address Not Allowed
- `2001`: No Active Key Found
- `9999`: Rate Limit Exceeded / General Error

### RotateKey
- `0000`: Success
- `1001`: Client Not Found or Inactive
- `2002`: ExpirationDays Required
- `2003`: RotationScheduleDays Required
- `2004`: No Active Key To Rotate
- `9999`: General Error

### RevokeKey
- `0000`: Success
- `1001`: Client Not Found or Inactive
- `2005`: No Active Keys To Revoke
- `9999`: General Error

### GetKeyUsageStats
- `0000`: Success
- `1001`: Client Not Found (warning, 통계는 계속 조회됨)
- `9999`: General Error

### CleanupExpiredKeys
- `0000`: Success
- `9999`: General Error

### CheckKeyRotationSchedule
- `0000`: Success
- `9999`: General Error

### GetClientInfo
- `0000`: Success
- `1001`: Client Not Found
- `9999`: General Error

### UpdateClientInfo
- `0000`: Success
- `1001`: Client Not Found
- `9999`: General Error

### InitializeRateLimitSettings
- `0000`: Success
- `9999`: General Error

---

## 💡 C# 구현 예시

```csharp
public class KmsErrorCode
{
    public const string Success = "0000";
    public const string ClientNotFound = "1001";
    public const string IPNotAllowed = "1002";
    public const string ClientIPDuplicate = "1003";
    public const string NoActiveKey = "2001";
    public const string ExpirationDaysRequired = "2002";
    public const string RotationScheduleDaysRequired = "2003";
    public const string NoKeyToRotate = "2004";
    public const string NoKeysToRevoke = "2005";
    public const string RateLimitExceeded = "9999";
    public const string GeneralError = "9999";
}

public class KmsResponse
{
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public bool IsSuccess => ErrorCode == KmsErrorCode.Success;
}

// 사용 예시
var result = await connection.QueryFirstAsync<KmsResponse>(
    "GetKey", 
    parameters, 
    commandType: CommandType.StoredProcedure
);

if (result.ErrorCode == KmsErrorCode.Success)
{
    // 성공 처리
    return Ok(result);
}
else if (result.ErrorCode == KmsErrorCode.IPNotAllowed)
{
    // IP 차단
    return Forbid(result.ErrorMessage);
}
else if (result.ErrorCode == KmsErrorCode.RateLimitExceeded)
{
    // Rate Limit
    return StatusCode(429, result.ErrorMessage);
}
else
{
    // 기타 에러
    return BadRequest(result.ErrorMessage);
}
```

---

**버전**: 1.0  
**마지막 업데이트**: 2024-01-01  
**작성자**: SECUiDEA KMS Team

