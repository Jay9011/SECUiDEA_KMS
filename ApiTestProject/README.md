# KMS API 테스트 (xUnit)

## 📋 개요

이 프로젝트는 SECUiDEA KMS API의 통합 테스트를 포함합니다 (xUnit 프레임워크 사용).

## 🚀 테스트 실행 방법

### 1. 사전 준비

KMS API 서버가 실행 중이어야 합니다:

```bash
cd SECUiDEA_KMS
dotnet run
```

### 2. 테스트용 클라이언트 GUID 설정

`appsettings.json` 파일을 편집하여 실제 등록된 클라이언트의 GUID를 설정:

```json
{
  "KmsApiSettings": {
    "BaseUrl": "https://localhost:7443",
    "TestClientGuid": "실제-GUID-여기에-입력"
  }
}
```

### 3. 테스트 실행

#### Visual Studio에서 실행
- Test Explorer에서 테스트 선택 후 실행

#### 명령줄에서 실행

전체 테스트 실행:
```bash
cd ApiTestProject
dotnet test
```

특정 카테고리만 실행:
```bash
# 키 생성 테스트만
dotnet test --filter "Category=KeyGeneration"

# 키 조회 테스트만
dotnet test --filter "Category=KeyRetrieval"

# 검증 테스트만
dotnet test --filter "Category=Validation"

# 통합 테스트만
dotnet test --filter "Category=Integration"
```

특정 테스트 메서드 실행:
```bash
dotnet test --filter "FullyQualifiedName~GenerateKey_PermanentKey_ReturnsSuccess"
```

상세 로그와 함께 실행:
```bash
dotnet test -v detailed
```

## 📝 테스트 케이스 목록

### 키 생성 테스트 (KeyGeneration)

| 테스트 메서드 | 설명 | 예상 결과 |
|-------------|------|----------|
| `GenerateKey_PermanentKey_ReturnsSuccess` | 반영구적 키 생성 (정상) | 200 OK |
| `GenerateKey_AutoRotationKey_ReturnsSuccess` | 자동 회전 키 생성 (정상) | 200 OK |
| `GenerateKey_WithoutHeader_ReturnsBadRequest` | X-Client-Guid 헤더 없이 요청 | 400 Bad Request |
| `GenerateKey_AutoRotationWithoutDays_ReturnsBadRequest` | 자동 회전 설정 불완전 | 400 Bad Request |

### 키 조회 테스트 (KeyRetrieval)

| 테스트 메서드 | 설명 | 예상 결과 |
|-------------|------|----------|
| `GetKey_WithValidGuid_ReturnsSuccessOrNotFound` | 활성 키 조회 (정상) | 200 OK 또는 404 Not Found |
| `GetKey_WithoutHeader_ReturnsBadRequest` | X-Client-Guid 헤더 없이 요청 | 400 Bad Request |
| `GetKey_WithInvalidGuid_ReturnsNotFoundOrForbidden` | 잘못된 GUID로 요청 | 404 Not Found 또는 403 Forbidden |

### 통합 테스트 (Integration)

| 테스트 메서드 | 설명 | 예상 결과 |
|-------------|------|----------|
| `FullFlow_GenerateAndRetrieveKey_Success` | 키 생성 → 조회 전체 플로우 | 모두 성공 |

## 🏗️ 프로젝트 구조

```
ApiTestProject/
├── ApiTests.cs              # 메인 테스트 클래스
├── KmsApiFixture.cs         # xUnit Fixture (공유 컨텍스트)
├── appsettings.json         # 설정 파일 (GUID, BaseUrl 등)
├── README.md                # 이 파일
└── ApiTestProject.csproj    # 프로젝트 파일
```

## 🔧 설정

### BaseUrl 변경

다른 환경에서 테스트하려면 `appsettings.json`의 `BaseUrl` 값을 변경:

```json
{
  "KmsApiSettings": {
    // 로컬 개발 환경
    "BaseUrl": "https://localhost:7443",
    
    // 또는 테스트 서버
    "BaseUrl": "https://test-kms.example.com",
    
    // 또는 프로덕션 서버
    "BaseUrl": "https://kms.example.com"
  }
}
```

### 환경 변수 사용

환경 변수로 설정을 오버라이드할 수 있습니다:

```bash
# Windows (PowerShell)
$env:KmsApiSettings__BaseUrl = "https://localhost:7443"
$env:KmsApiSettings__TestClientGuid = "your-guid-here"
dotnet test

# Linux/Mac
export KmsApiSettings__BaseUrl="https://localhost:7443"
export KmsApiSettings__TestClientGuid="your-guid-here"
dotnet test
```

### 환경별 설정 파일

다른 환경을 위한 설정 파일을 만들 수 있습니다:

```bash
# appsettings.Development.json
{
  "KmsApiSettings": {
    "BaseUrl": "https://localhost:7443",
    "TestClientGuid": "dev-guid"
  }
}

# appsettings.Staging.json
{
  "KmsApiSettings": {
    "BaseUrl": "https://test-kms.example.com",
    "TestClientGuid": "staging-guid"
  }
}
```

## 🎯 xUnit 특징

### Fixture 사용

이 프로젝트는 `IClassFixture<KmsApiFixture>`를 사용하여:
- 모든 테스트가 동일한 HttpClient 인스턴스를 공유
- 테스트 성능 향상 (초기화 1회만 수행)
- 설정 파일을 한 번만 로드

### Trait 사용

테스트를 카테고리로 분류:

```csharp
[Fact]
[Trait("Category", "KeyGeneration")]
[Trait("Category", "Validation")]
public async Task TestMethod() { }
```

### Assert 메서드

xUnit의 Assert는 간결합니다:
- `Assert.Equal(expected, actual)`
- `Assert.NotNull(value)`
- `Assert.True(condition)`
- `Assert.Contains(substring, text)`

## ⚠️ 주의사항

1. **실제 데이터 사용**: 이 테스트는 실제 데이터베이스와 연동됩니다. 테스트용 클라이언트를 사용하세요.

2. **IP 검증**: API는 IP 검증을 수행합니다. 테스트를 실행하는 머신의 IP가 클라이언트에 등록된 IP와 일치해야 합니다.
   - Strict 모드: IP가 정확히 일치해야 함
   - Lenient 모드: IP 검증 무시

3. **Rate Limiting**: 동일한 IP에서 너무 많은 요청을 보내면 Rate Limit에 걸릴 수 있습니다.

4. **병렬 실행**: xUnit은 기본적으로 테스트를 병렬로 실행합니다. 순서에 의존하는 테스트는 작성하지 마세요.

## 🐛 문제 해결

### "appsettings.json에 유효한 TestClientGuid를 설정해야 합니다"

→ `appsettings.json` 파일에서 `TestClientGuid`를 실제 GUID로 설정하세요.

### "Client not found or inactive" (1001 에러)

→ 설정한 GUID가 데이터베이스에 등록되어 있고 활성 상태인지 확인하세요.

### "IP address not allowed" (1002 에러)

→ 클라이언트 설정에서 IP Validation Mode를 "Lenient"로 변경하거나, 현재 머신의 IP를 클라이언트에 등록하세요.

### SSL 인증서 오류

→ 개발 인증서를 신뢰하도록 설정:

```bash
dotnet dev-certs https --trust
```

### appsettings.json이 복사되지 않음

→ 프로젝트를 다시 빌드:

```bash
dotnet build
```

## 📊 테스트 결과 보기

테스트 결과를 파일로 저장:

```bash
# TRX 형식
dotnet test --logger "trx;LogFileName=test-results.trx"

# HTML 형식 (추가 패키지 필요)
dotnet test --logger "html;LogFileName=test-results.html"
```

## 📚 추가 리소스

- [xUnit 문서](https://xunit.net/)
- [.NET 테스트 가이드](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [xUnit vs MSTest vs NUnit](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

