using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace APITests;

/// <summary>
/// KMS API 통합 테스트
/// </summary>
[TestClass]
public sealed class KMSTests
{
    #region 초기화
    private static HttpClient? _httpClient;
    private static string _baseUrl = "https://localhost:7001"; // 기본값
    private static Guid _testClientGuid = Guid.Empty;

    /// <summary>
    /// 테스트 클래스 초기화 (모든 테스트 전에 한 번 실행)
    /// </summary>
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        // appsettings.json이나 환경 변수에서 설정 읽기
        _baseUrl = context.Properties["BaseUrl"]?.ToString() ?? _baseUrl;

        // 테스트용 클라이언트 GUID 설정
        var guidFromConfig = context.Properties["TestClientGuid"]?.ToString();
        if (!string.IsNullOrEmpty(guidFromConfig) && Guid.TryParse(guidFromConfig, out var parsedGuid))
        {
            _testClientGuid = parsedGuid;
        }

        // HttpClient 초기화
        var handler = new HttpClientHandler
        {
            // 개발 환경에서 SSL 인증서 검증 무시 (프로덕션에서는 제거)
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Console.WriteLine($"테스트 초기화 완료: BaseUrl={_baseUrl}");
    }

    /// <summary>
    /// 테스트 클래스 정리 (모든 테스트 후에 한 번 실행)
    /// </summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    /// 각 테스트 메서드 실행 전 초기화
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        // 테스트별로 필요한 초기화
        Assert.IsNotNull(_httpClient, "HttpClient가 초기화되지 않았습니다.");

        // 테스트용 GUID가 설정되지 않은 경우 경고
        if (_testClientGuid == Guid.Empty)
        {
            Console.WriteLine("경고: 테스트용 ClientGuid가 설정되지 않았습니다. SetTestClientGuid()를 호출하거나 runsettings를 설정하세요.");
        }
    }
    
    #endregion

    #region 헬퍼 메서드

    /// <summary>
    /// 테스트용 클라이언트 GUID 설정
    /// </summary>
    /// <param name="clientGuid">실제 등록된 클라이언트의 GUID</param>
    public static void SetTestClientGuid(Guid clientGuid)
    {
        _testClientGuid = clientGuid;
        Console.WriteLine($"테스트 ClientGuid 설정됨: {_testClientGuid}");
    }

    /// <summary>
    /// 테스트용 클라이언트 GUID 설정 (문자열)
    /// </summary>
    public static void SetTestClientGuid(string clientGuid)
    {
        if (Guid.TryParse(clientGuid, out var guid))
        {
            SetTestClientGuid(guid);
        }
        else
        {
            throw new ArgumentException("유효하지 않은 GUID 형식입니다.", nameof(clientGuid));
        }
    }

    #endregion

    #region 키 생성 테스트

    /// <summary>
    /// 반영구적 키 생성 테스트 (정상 케이스)
    /// </summary>
    [TestMethod]
    [TestCategory("KeyGeneration")]
    public async Task GenerateKey_PermanentKey_Success()
    {
        // Arrange
        var request = new
        {
            isAutoRotation = false,
            expirationDays = (int?)null,
            rotationScheduleDays = (int?)null
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/keys/generate")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        var response = await _httpClient!.SendAsync(httpRequest);

        // Assert
        Assert.IsNotNull(response, "응답이 null입니다.");

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"응답 코드: {response.StatusCode}");
        Console.WriteLine($"응답 내용: {content}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"예상: 200 OK, 실제: {response.StatusCode}");

        // 응답 본문 검증
        var result = JsonSerializer.Deserialize<KmsResponse<EncryptionKeyEntity>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result, "응답 역직렬화 실패");
        Assert.AreEqual("0000", result.ErrorCode, $"에러 코드: {result.ErrorCode}, 메시지: {result.ErrorMessage}");
        Assert.IsNotNull(result.Data, "키 데이터가 null입니다.");
        Assert.IsTrue(result.Data.KeyId > 0, "KeyId가 유효하지 않습니다.");

        Console.WriteLine($"생성된 키 ID: {result.Data.KeyId}, 버전: {result.Data.KeyVersion}");
    }

    /// <summary>
    /// 자동 회전 키 생성 테스트 (정상 케이스)
    /// </summary>
    [TestMethod]
    [TestCategory("KeyGeneration")]
    public async Task GenerateKey_AutoRotationKey_Success()
    {
        // Arrange
        var request = new
        {
            isAutoRotation = true,
            expirationDays = 90,
            rotationScheduleDays = 30
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/keys/generate")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        var response = await _httpClient!.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"응답 코드: {response.StatusCode}");
        Console.WriteLine($"응답 내용: {content}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"예상: 200 OK, 실제: {response.StatusCode}");

        var result = JsonSerializer.Deserialize<KmsResponse<EncryptionKeyEntity>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result, "응답 역직렬화 실패");
        Assert.AreEqual("0000", result.ErrorCode, $"에러 코드: {result.ErrorCode}, 메시지: {result.ErrorMessage}");
        Assert.IsNotNull(result.Data, "키 데이터가 null입니다.");
        Assert.IsTrue(result.Data.IsAutoRotation, "자동 회전 플래그가 false입니다.");
        Assert.AreEqual(30, result.Data.RotationScheduleDays, "회전 스케줄이 일치하지 않습니다.");

        Console.WriteLine($"생성된 자동 회전 키 ID: {result.Data.KeyId}, 만료일: {result.Data.ExpiresAt}");
    }

    /// <summary>
    /// X-Client-Guid 헤더 없이 키 생성 요청 (실패 케이스)
    /// </summary>
    [TestMethod]
    [TestCategory("KeyGeneration")]
    [TestCategory("Validation")]
    public async Task GenerateKey_WithoutHeader_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            isAutoRotation = false,
            expirationDays = (int?)null,
            rotationScheduleDays = (int?)null
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/keys/generate")
        {
            Content = JsonContent.Create(request)
        };
        // X-Client-Guid 헤더를 추가하지 않음

        var response = await _httpClient!.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"응답 코드: {response.StatusCode}");
        Console.WriteLine($"응답 내용: {content}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "헤더 없이 요청 시 400 Bad Request를 반환해야 합니다.");

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result);
        Assert.AreEqual("9999", result.ErrorCode);
        Assert.IsTrue(result.ErrorMessage.Contains("X-Client-Guid"),
            "에러 메시지에 'X-Client-Guid'가 포함되어야 합니다.");
    }

    /// <summary>
    /// 자동 회전 설정 불완전 시 키 생성 요청 (실패 케이스)
    /// </summary>
    [TestMethod]
    [TestCategory("KeyGeneration")]
    [TestCategory("Validation")]
    public async Task GenerateKey_AutoRotationWithoutDays_ReturnsBadRequest()
    {
        // Arrange - ExpirationDays 없음
        var request = new
        {
            isAutoRotation = true,
            expirationDays = (int?)null,
            rotationScheduleDays = 30
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/keys/generate")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        var response = await _httpClient!.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"응답 코드: {response.StatusCode}");
        Console.WriteLine($"응답 내용: {content}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "ExpirationDays 없이 자동 회전 요청 시 400 Bad Request를 반환해야 합니다.");

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result);
        Assert.AreEqual("2002", result.ErrorCode);
    }

    #endregion

    #region 키 조회 테스트

    /// <summary>
    /// 활성 키 조회 테스트 (정상 케이스)
    /// </summary>
    [TestMethod]
    [TestCategory("KeyRetrieval")]
    public async Task GetKey_WithValidGuid_Success()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        httpRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        // Act
        var response = await _httpClient!.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"응답 코드: {response.StatusCode}");
        Console.WriteLine($"응답 내용: {content}");

        // 키가 존재하면 200, 없으면 404
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"예상: 200 또는 404, 실제: {response.StatusCode}");

        var result = JsonSerializer.Deserialize<KmsResponse<string>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result, "응답 역직렬화 실패");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.AreEqual("0000", result.ErrorCode);
            Assert.IsNotNull(result.Data, "키 데이터가 null입니다.");
            Assert.IsFalse(string.IsNullOrEmpty(result.Data), "키 값이 비어있습니다.");
            Console.WriteLine($"조회된 키 길이: {result.Data.Length} 문자");
        }
        else
        {
            Assert.AreEqual("2001", result.ErrorCode, "활성 키가 없을 때 에러 코드는 2001이어야 합니다.");
            Console.WriteLine("활성 키가 없습니다. 먼저 키를 생성하세요.");
        }
    }

    /// <summary>
    /// X-Client-Guid 헤더 없이 키 조회 요청 (실패 케이스)
    /// </summary>
    [TestMethod]
    [TestCategory("KeyRetrieval")]
    [TestCategory("Validation")]
    public async Task GetKey_WithoutHeader_ReturnsBadRequest()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        // X-Client-Guid 헤더를 추가하지 않음

        // Act
        var response = await _httpClient!.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"응답 코드: {response.StatusCode}");
        Console.WriteLine($"응답 내용: {content}");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            "헤더 없이 요청 시 400 Bad Request를 반환해야 합니다.");

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result);
        Assert.AreEqual("9999", result.ErrorCode);
        Assert.IsTrue(result.ErrorMessage.Contains("X-Client-Guid"),
            "에러 메시지에 'X-Client-Guid'가 포함되어야 합니다.");
    }

    /// <summary>
    /// 잘못된 GUID로 키 조회 요청 (실패 케이스)
    /// </summary>
    [TestMethod]
    [TestCategory("KeyRetrieval")]
    [TestCategory("Validation")]
    public async Task GetKey_WithInvalidGuid_ReturnsNotFound()
    {
        // Arrange
        var invalidGuid = Guid.NewGuid(); // 존재하지 않는 GUID
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        httpRequest.Headers.Add("X-Client-Guid", invalidGuid.ToString());

        // Act
        var response = await _httpClient!.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"응답 코드: {response.StatusCode}");
        Console.WriteLine($"응답 내용: {content}");

        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
            $"예상: 404 또는 403, 실제: {response.StatusCode}");

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result);
        Assert.IsTrue(result.ErrorCode == "1001" || result.ErrorCode == "1002",
            $"클라이언트를 찾을 수 없거나 IP 불일치 에러여야 합니다. 실제: {result.ErrorCode}");
    }

    #endregion

    #region 통합 시나리오 테스트

    /// <summary>
    /// 전체 플로우 테스트: 키 생성 → 키 조회
    /// </summary>
    [TestMethod]
    [TestCategory("Integration")]
    public async Task FullFlow_GenerateAndRetrieveKey_Success()
    {
        // Step 1: 키 생성
        var generateRequest = new
        {
            isAutoRotation = false,
            expirationDays = (int?)null,
            rotationScheduleDays = (int?)null
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/keys/generate")
        {
            Content = JsonContent.Create(generateRequest)
        };
        httpRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        var generateResponse = await _httpClient!.SendAsync(httpRequest);
        var generateContent = await generateResponse.Content.ReadAsStringAsync();

        Console.WriteLine($"[키 생성] 응답 코드: {generateResponse.StatusCode}");
        Console.WriteLine($"[키 생성] 응답 내용: {generateContent}");

        Assert.AreEqual(HttpStatusCode.OK, generateResponse.StatusCode, "키 생성 실패");

        var generateResult = JsonSerializer.Deserialize<KmsResponse<EncryptionKeyEntity>>(generateContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(generateResult?.Data);
        var keyId = generateResult.Data.KeyId;
        Console.WriteLine($"생성된 키 ID: {keyId}");

        // Step 2: 생성된 키 조회
        await Task.Delay(1000); // 1초 대기 (DB 반영 시간)

        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        getRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        var getResponse = await _httpClient.SendAsync(getRequest);
        var getContent = await getResponse.Content.ReadAsStringAsync();

        Console.WriteLine($"[키 조회] 응답 코드: {getResponse.StatusCode}");
        Console.WriteLine($"[키 조회] 응답 내용: {getContent}");

        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode, "키 조회 실패");

        var getResult = JsonSerializer.Deserialize<KmsResponse<string>>(getContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(getResult?.Data);
        Assert.IsFalse(string.IsNullOrEmpty(getResult.Data), "조회된 키가 비어있습니다.");

        Console.WriteLine("✅ 전체 플로우 테스트 성공!");
    }

    #endregion
}

#region 응답 모델 (테스트용)

/// <summary>
/// KMS 응답 기본 모델
/// </summary>
public class KmsResponse
{
    public string ErrorCode { get; set; } = "0000";
    public string ErrorMessage { get; set; } = "Success";
}

/// <summary>
/// KMS 응답 제네릭 모델
/// </summary>
public class KmsResponse<T> : KmsResponse
{
    public T? Data { get; set; }
}

/// <summary>
/// 암호화 키 엔티티 (테스트용 간소화 버전)
/// </summary>
public class EncryptionKeyEntity
{
    public long KeyId { get; set; }
    public int ClientId { get; set; }
    public string KeyStatus { get; set; } = string.Empty;
    public bool IsAutoRotation { get; set; }
    public int? RotationScheduleDays { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int KeyVersion { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion
