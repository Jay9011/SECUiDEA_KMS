# KMS API 테스트 가이드

## 📋 개요

이 프로젝트는 SECUiDEA KMS API의 통합 테스트를 포함합니다.

## 🚀 테스트 실행 방법

### 1. 사전 준비

KMS API 서버가 실행 중이어야 합니다:

```bash
cd SECUiDEA_KMS
dotnet run
```

### 2. 테스트용 클라이언트 GUID 설정

테스트를 실행하기 전에 **실제 등록된 클라이언트의 GUID**를 설정해야 합니다.

#### 방법 1: runsettings 파일 사용 (권장)

`test.runsettings` 파일을 편집하여 `TestClientGuid` 값을 변경:

```xml
<Parameter name="TestClientGuid" value="YOUR-CLIENT-GUID-HERE" />
```

그리고 Visual Studio에서:
1. Test > Configure Run Settings > Select Solution Wide runsettings File
2. `test.runsettings` 파일 선택

또는 명령줄에서:

```bash
dotnet test --settings test.runsettings
```

#### 방법 2: 코드에서 직접 설정

테스트 클래스에서 직접 GUID 설정:

```csharp
[ClassInitialize]
public static void ClassInitialize(TestContext context)
{
    // 여기에 실제 GUID 입력
    KMSTests.SetTestClientGuid("your-guid-here");
    
    // 또는
    KMSTests.SetTestClientGuid(new Guid("your-guid-here"));
}
```

### 3. 테스트 실행

#### Visual Studio에서 실행
- Test Explorer에서 테스트 선택 후 실행

#### 명령줄에서 실행

전체 테스트 실행:
```bash
dotnet test
```

특정 카테고리만 실행:
```bash
# 키 생성 테스트만
dotnet test --filter "TestCategory=KeyGeneration"

# 키 조회 테스트만
dotnet test --filter "TestCategory=KeyRetrieval"

# 검증 테스트만
dotnet test --filter "TestCategory=Validation"

# 통합 테스트만
dotnet test --filter "TestCategory=Integration"
```

특정 테스트 메서드 실행:
```bash
dotnet test --filter "FullyQualifiedName~GenerateKey_PermanentKey_Success"
```

## 📝 테스트 케이스 목록

### 키 생성 테스트 (KeyGeneration)

| 테스트 메서드 | 설명 | 예상 결과 |
|-------------|------|----------|
| `GenerateKey_PermanentKey_Success` | 반영구적 키 생성 (정상) | 200 OK |
| `GenerateKey_AutoRotationKey_Success` | 자동 회전 키 생성 (정상) | 200 OK |
| `GenerateKey_WithoutHeader_ReturnsBadRequest` | X-Client-Guid 헤더 없이 요청 | 400 Bad Request |
| `GenerateKey_AutoRotationWithoutDays_ReturnsBadRequest` | 자동 회전 설정 불완전 | 400 Bad Request |

### 키 조회 테스트 (KeyRetrieval)

| 테스트 메서드 | 설명 | 예상 결과 |
|-------------|------|----------|
| `GetKey_WithValidGuid_Success` | 활성 키 조회 (정상) | 200 OK 또는 404 Not Found |
| `GetKey_WithoutHeader_ReturnsBadRequest` | X-Client-Guid 헤더 없이 요청 | 400 Bad Request |
| `GetKey_WithInvalidGuid_ReturnsNotFound` | 잘못된 GUID로 요청 | 404 Not Found 또는 403 Forbidden |

### 통합 테스트 (Integration)

| 테스트 메서드 | 설명 | 예상 결과 |
|-------------|------|----------|
| `FullFlow_GenerateAndRetrieveKey_Success` | 키 생성 → 조회 전체 플로우 | 모두 성공 |

## 🔧 설정

### BaseUrl 변경

다른 환경에서 테스트하려면 `test.runsettings`의 `BaseUrl` 값을 변경:

```xml
<!-- 로컬 개발 환경 -->
<Parameter name="BaseUrl" value="https://localhost:7001" />

<!-- 테스트 서버 -->
<Parameter name="BaseUrl" value="https://test-kms.example.com" />

<!-- 프로덕션 서버 -->
<Parameter name="BaseUrl" value="https://kms.example.com" />
```

### SSL 인증서 검증

개발 환경에서는 자체 서명 인증서를 사용하므로 SSL 검증을 무시합니다.
프로덕션 환경에서는 `KMSTests.cs`의 다음 코드를 제거하세요:

```csharp
var handler = new HttpClientHandler
{
    // 이 부분을 제거하거나 조건부로 설정
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
};
```

## 📊 테스트 결과 확인

테스트 실행 후 콘솔에서 상세 로그를 확인할 수 있습니다:

```
응답 코드: OK
응답 내용: {"errorCode":"0000","errorMessage":"Success","data":{...}}
생성된 키 ID: 123, 버전: 1
```

## ⚠️ 주의사항

1. **실제 데이터 사용**: 이 테스트는 실제 데이터베이스와 연동됩니다. 테스트용 클라이언트를 사용하세요.

2. **IP 검증**: API는 IP 검증을 수행합니다. 테스트를 실행하는 머신의 IP가 클라이언트에 등록된 IP와 일치해야 합니다.
   - Strict 모드: IP가 정확히 일치해야 함
   - Lenient 모드: IP 검증 무시

3. **Rate Limiting**: 동일한 IP에서 너무 많은 요청을 보내면 Rate Limit에 걸릴 수 있습니다.

4. **병렬 실행**: 테스트가 병렬로 실행되므로 순서에 의존하는 테스트는 작성하지 마세요.

## 🐛 문제 해결

### "테스트용 ClientGuid가 설정되지 않았습니다"

→ `test.runsettings` 파일에서 `TestClientGuid`를 설정하거나 `SetTestClientGuid()` 메서드를 호출하세요.

### "Client not found or inactive" (1001 에러)

→ 설정한 GUID가 데이터베이스에 등록되어 있고 활성 상태인지 확인하세요.

### "IP address not allowed" (1002 에러)

→ 클라이언트 설정에서 IP Validation Mode를 "Lenient"로 변경하거나, 현재 머신의 IP를 클라이언트에 등록하세요.

### SSL 인증서 오류

→ 개발 인증서를 신뢰하도록 설정:

```bash
dotnet dev-certs https --trust
```

## 📚 추가 리소스

- [MSTest 문서](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [xUnit vs MSTest](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

