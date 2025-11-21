# SECUiDEA KMS 데이터베이스 스키마

## 📁 폴더 구조

```
Database/
├── 0. Tables/          # 테이블 정의 SQL 스크립트
├── 5. Procedures/      # 저장 프로시저 SQL 스크립트
├── ERROR_CODES.md     # 에러 코드 정의
└── README.md          # 이 문서
```

## 📊 테이블 구조

### 0. ClientServers
클라이언트 서버 정보를 관리하는 테이블입니다.
- **PK**: `ClientId` (INT, IDENTITY)
- **Unique**: `ClientGuid` (서버에서 자동 생성)
- 클라이언트 등록 시 IP 주소 필수
- **AllowedIPAddresses 제거**: 단일 IP만 사용
- IP 검증 모드: Strict, None
- **IP 중복 등록 방지**: 동일 IP는 한 번만 등록 가능

### 0. RateLimits
요청 제한을 관리하는 테이블입니다.
- 과도한 요청 방지
- IP별 요청 횟수 추적
- 차단 정보 저장
- 7일 이전 기록 자동 정리

### 0. RateLimitsSettings
Rate Limit 설정을 관리하는 테이블입니다.
- 시간 윈도우 (기본 60초)
- 최대 요청 수 (기본 100회)
- 활성화/비활성화

### 1. EncryptionKeys
암호화 키를 마스터 키로 암호화하여 저장합니다.
- **PK**: `KeyId` (INT, IDENTITY)
- **FK**: `ClientId` → ClientServers
- 자동 회전 vs 반영구적 키 지원
- 버전 관리 및 만료일 추적

### 3. KeyRequests
모든 키 요청을 로깅합니다.
- HttpContext 정보 (IP, UserAgent, Host 등) 저장
- 성공/실패 여부 및 응답 시간 기록

### 3. KeyRotationHistory
키 회전 이력을 추적합니다.
- 이전 키와 새 키의 관계 기록
- 회전 유형 및 사유 저장

### 5. AuditLogs
시스템 전체의 중요 이벤트를 기록합니다.
- 심각도 레벨 구분 (Info, Warning, Error, Critical)
- JSON 형태로 상세 정보 저장 (수동 생성, SQL Server 2016+ 호환)

## 🔧 저장 프로시저

### 일관된 반환 형식
모든 프로시저는 다음 형식으로 ErrorCode와 ErrorMessage를 반환합니다:

```sql
SELECT 
    @ErrorCode AS ErrorCode,      -- '0000': 성공, 기타: 에러 코드
    @ErrorMessage AS ErrorMessage, -- 'Success' 또는 에러 메시지
    -- ... 추가 반환 데이터
```

### 주요 프로시저 (프로시저명은 CamelCase, sp_ 접두사 없음)

#### 00. 내부 유틸리티
- **CheckRateLimit**: Rate Limit 검증 (OUTPUT 파라미터로 결과 반환)

#### 클라이언트 관리
1. **RegisterClient**: 새 클라이언트 등록 (GUID 자동 생성) ⭐
2. **GetClientInfo**: 클라이언트 정보 조회
3. **UpdateClientInfo**: 클라이언트 정보 업데이트

#### 키 관리
4. **GenerateKey**: 암호화 키 생성 ⭐
5. **GetKey**: 키 조회 ⭐
6. **RotateKey**: 키 회전
7. **RevokeKey**: 키 폐기

#### 통계 및 유틸리티
8. **GetKeyUsageStats**: 사용 통계
9. **CleanupExpiredKeys**: 만료 키 정리
10. **CheckKeyRotationSchedule**: 회전 일정 확인
11. **InitializeRateLimitSettings**: Rate Limit 설정 초기화

## 📋 에러 코드

자세한 내용은 `ERROR_CODES.md` 참조

### 주요 에러 코드
- `0000`: Success
- `1001`: Client Not Found or Inactive
- `1002`: IP Address Not Allowed
- `1003`: ClientIP Already Registered
- `2001`: No Active Key Found
- `2002`: ExpirationDays Required
- `2003`: RotationScheduleDays Required
- `2004`: No Active Key To Rotate
- `2005`: No Active Keys To Revoke
- `9999`: Rate Limit Exceeded / General Error

## 🚀 배포 순서

```sql
-- 1. 테이블 생성 (순서대로)
:r "0. Tables\0. ClientServers.sql"
:r "0. Tables\0. RateLimits.sql"
:r "0. Tables\0. RateLimitsSettings.sql"
:r "0. Tables\1. EncryptionKeys.sql"
:r "0. Tables\3. KeyRequests.sql"
:r "0. Tables\3. KeyRotationHistory.sql"
:r "0. Tables\5. AuditLogs.sql"

-- 2. 프로시저 생성 (순서대로 - CheckRateLimit은 먼저)
:r "5. Procedures\00_CheckRateLimit.sql"
:r "5. Procedures\01_RegisterClient.sql"
:r "5. Procedures\02_GenerateKey.sql"
:r "5. Procedures\03_GetKey.sql"
:r "5. Procedures\04_RotateKey.sql"
:r "5. Procedures\05_RevokeKey.sql"
:r "5. Procedures\06_GetKeyUsageStats.sql"
:r "5. Procedures\07_CleanupExpiredKeys.sql"
:r "5. Procedures\08_CheckKeyRotationSchedule.sql"
:r "5. Procedures\09_GetClientInfo.sql"
:r "5. Procedures\10_UpdateClientInfo.sql"
:r "5. Procedures\11_InitializeRateLimitSettings.sql"

-- 3. Rate Limit 설정 초기화
EXEC InitializeRateLimitSettings 
    @RateLimitSeconds = 60,
    @MaxRequests = 100,
    @CreatedBy = 'System';
```

## 🎯 사용 시나리오

### 1단계: 관리자가 클라이언트 등록

```sql
DECLARE @ClientId INT;
DECLARE @ClientGuid UNIQUEIDENTIFIER;

EXEC RegisterClient 
    @ClientName = 'Production Server',
    @ClientIP = '192.168.1.100',
    @Description = '운영 서버',
    @IPValidationMode = 'Strict',
    @CreatedBy = 'Admin',
    @ClientId = @ClientId OUTPUT,
    @ClientGuid = @ClientGuid OUTPUT;

-- 반환 예시:
-- ErrorCode: '0000'
-- ErrorMessage: 'Success'
-- ClientId: 1
-- ClientGuid: 'abc-123-def-456'
```

### 2단계: 관리자가 GUID를 클라이언트에게 전달

이메일, 메신저, 설정 파일 등을 통해 안전하게 전달

### 3단계: 클라이언트가 키 생성 요청

```sql
DECLARE @KeyId BIGINT;

EXEC GenerateKey 
    @ClientGuid = 'abc-123-def-456',
    @EncryptedKeyData = 0x..., -- 암호화된 키 데이터
    @IsAutoRotation = 0, -- 반영구적
    @RequestIP = '192.168.1.100',
    @RequestUserAgent = 'MyApp/1.0',
    @RequestHost = 'kms.example.com',
    @KeyId = @KeyId OUTPUT;

-- 반환 예시:
-- ErrorCode: '0000'
-- ErrorMessage: 'Success'
-- KeyId: 1
```

### 4단계: 클라이언트가 키 조회

```sql
EXEC GetKey 
    @ClientGuid = 'abc-123-def-456',
    @RequestIP = '192.168.1.100',
    @RequestUserAgent = 'MyApp/1.0',
    @RequestHost = 'kms.example.com',
    @RequestPath = '/api/keys';

-- 반환 예시:
-- ErrorCode: '0000'
-- ErrorMessage: 'Success'
-- Key: (VARBINARY 암호화된 데이터)
-- CreatedAt: '2024-01-01 00:00:00'
-- ExpiresAt: '2034-01-01 00:00:00'
```

## 💡 주요 특징

### 1. **일관된 에러 처리**
- 모든 프로시저가 ErrorCode와 ErrorMessage 반환
- 백엔드에서 마지막 SELECT 결과로 상태 확인 가능
- 에러 코드로 세밀한 에러 처리 가능

### 2. **단순화된 IP 관리**
- AllowedIPAddresses 제거, ClientIP 하나만 사용
- IPValidationMode로 검증 모드 제어
  - `Strict`: RegisteredIP와 RequestIP 일치 필요
  - `None`: IP 검증 안 함

### 3. **자동 GUID 생성**
- 관리자가 IP만 등록하면 서버가 자동으로 GUID 생성
- GUID는 관리자가 안전한 매체로 클라이언트에게 전달
- GUID 충돌 방지 (서버가 보장)

### 4. **IP 중복 등록 방지**
- 동일 IP로는 한 번만 등록 가능
- 관리자가 직접 관리 및 통제

### 5. **Rate Limiting**
- IP별 요청 제한
- 설정 가능한 시간 윈도우 및 최대 요청 수
- 차단 시 자동 15분 블록
- 오래된 기록 자동 정리 (7일)

### 6. **상세한 보안 로깅**
- 모든 요청을 KeyRequests 테이블에 기록
- 보안 이벤트는 AuditLogs에 별도 기록
  - `UnauthorizedKeyAccess`: 인증되지 않은 키 접근
  - `IPMismatch`: IP 불일치
  - `RateLimitExceeded`: 요청 제한 초과

### 7. **SQL Server 호환성**
- JSON 생성을 수동으로 처리 (SQL Server 2016+ 호환)
- JSON_OBJECT 대신 문자열 연결 사용

## 🔒 보안 고려사항

### 1. **IP 기반 인증**
- 등록된 IP만 키 접근 가능
- IP 검증 모드로 유연성 확보

### 2. **Rate Limiting**
- DDoS 공격 방지
- 무차별 대입 공격 차단
- 자동 블록 및 해제

### 3. **감사 추적**
- 모든 중요 작업 기록
- 실패한 접근 시도 로깅
- 보안 사고 조사 지원

### 4. **키 암호화**
- 모든 키는 마스터 키로 암호화되어 저장
- 데이터베이스 유출 시에도 키 보호
- 버전 관리로 이전 키 복호화 지원

### 5. **GUID 전달 보안**
- GUID는 안전한 채널로만 전달
- 최초 1회만 전달 (클라이언트에서 저장)

## 📝 C# API 구현 예시

```csharp
// 프로시저 호출 및 ErrorCode 처리
public async Task<KmsResponse<ClientInfo>> RegisterClientAsync(RegisterClientRequest request)
{
    using var connection = new SqlConnection(_connectionString);
    
    var parameters = new DynamicParameters();
    parameters.Add("@ClientName", request.ClientName);
    parameters.Add("@ClientIP", request.ClientIP);
    parameters.Add("@Description", request.Description);
    parameters.Add("@IPValidationMode", request.ValidationMode ?? "Strict");
    parameters.Add("@CreatedBy", User.Identity?.Name);
    parameters.Add("@ClientId", dbType: DbType.Int32, direction: ParameterDirection.Output);
    parameters.Add("@ClientGuid", dbType: DbType.Guid, direction: ParameterDirection.Output);
    
    var result = await connection.QueryFirstAsync<dynamic>(
        "RegisterClient", 
        parameters, 
        commandType: CommandType.StoredProcedure
    );
    
    // ErrorCode 처리
    if (result.ErrorCode == "0000")
    {
        return new KmsResponse<ClientInfo>
        {
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            Data = new ClientInfo
            {
                ClientId = result.ClientId,
                ClientGuid = result.ClientGuid
            }
        };
    }
    else
    {
        return new KmsResponse<ClientInfo>
        {
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            Data = null
        };
    }
}

// 에러 코드에 따른 HTTP 상태 코드 매핑
public IActionResult MapKmsError(string errorCode, string errorMessage)
{
    return errorCode switch
    {
        "0000" => Ok(errorMessage),
        "1001" => NotFound(errorMessage),
        "1002" => Forbid(errorMessage),
        "1003" => Conflict(errorMessage),
        "2001" => NotFound(errorMessage),
        "9999" => errorMessage.Contains("Rate limit") 
            ? StatusCode(429, errorMessage)
            : BadRequest(errorMessage),
        _ => StatusCode(500, errorMessage)
    };
}
```

## 📅 유지보수

### 정기 실행 권장 프로시저

```sql
-- 매일 실행: 만료된 키 정리
EXEC CleanupExpiredKeys @RetentionDays = 90;

-- 매일 실행: 회전 일정 확인
EXEC CheckKeyRotationSchedule @DaysBeforeExpiration = 7;
```

SQL Server Agent Job으로 스케줄링을 권장합니다.

## 🎓 워크플로우 요약

```
[관리자] 
  ↓ RegisterClient 실행
  ↓ (ClientIP 등록 → GUID 자동 생성)
  ↓
[서버가 GUID 생성]
  ↓ ErrorCode = '0000' 확인
  ↓
[관리자가 GUID를 안전한 매체로 전달]
  ↓ (이메일, 메신저, 설정 파일 등)
  ↓
[클라이언트]
  ↓ appsettings.json에 GUID 저장
  ↓
  ↓ GenerateKey(GUID, IP)
  ↓ ErrorCode 확인 → Rate Limit 체크 → IP 검증 → 키 생성
  ↓
  ↓ 키 수신 및 사용
  ↓
  ↓ GetKey(GUID, IP) - 필요시
  ↓ ErrorCode 확인 → 키 반환
```

## ⚠️ 중요 보안 권장사항

1. **HTTPS 필수**: 모든 통신은 TLS 1.2+ 사용
2. **키 메모리 저장**: 클라이언트는 키를 디스크에 저장하지 말 것
3. **정기 감사**: AuditLogs를 주기적으로 검토
4. **GUID 안전 전달**: 암호화된 채널로 전달, 로그에 기록하지 말 것
5. **ErrorCode 검증**: 모든 API 호출 후 ErrorCode를 확인

## 🔧 변경 이력

### v1.0 (2024-01-01)
- 초기 설계
- AllowedIPAddresses 제거, ClientIP 단일 사용
- 일관된 ErrorCode/ErrorMessage 반환 형식 적용
- 에러 코드 체계 정의

---

**버전**: 1.0  
**마지막 업데이트**: 2024-01-01  
**작성자**: SECUiDEA KMS Team
