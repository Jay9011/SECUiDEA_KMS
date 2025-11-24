using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace ApiTestProject;

/// <summary>
/// KMS API 통합 테스트 (xUnit)
/// </summary>
public class ApiTests : IClassFixture<KmsApiFixture>
{
    private readonly KmsApiFixture _fixture;
    private readonly HttpClient _httpClient;
    private readonly Guid _testClientGuid;
    private readonly ITestOutputHelper _output;

    public ApiTests(KmsApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _httpClient = fixture.HttpClient;
        _testClientGuid = fixture.TestClientGuid;
        _output = output;
    }

    #region 키 생성 테스트

    /// <summary>
    /// 반영구적 키 생성 테스트 (정상 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyGeneration")]
    public async Task GenerateKey_PermanentKey_ReturnsSuccess()
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

        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = JsonSerializer.Deserialize<KmsResponse<EncryptionKeyEntity>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("0000", result.ErrorCode);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.KeyId > 0);
    }

    /// <summary>
    /// 자동 회전 키 생성 테스트 (정상 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyGeneration")]
    public async Task GenerateKey_AutoRotationKey_ReturnsSuccess()
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

        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = JsonSerializer.Deserialize<KmsResponse<EncryptionKeyEntity>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("0000", result.ErrorCode);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsAutoRotation);
        Assert.Equal(30, result.Data.RotationScheduleDays);
    }

    /// <summary>
    /// X-Client-Guid 헤더 없이 키 생성 요청 (실패 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyGeneration")]
    [Trait("Category", "Validation")]
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

        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("9999", result.ErrorCode);
        Assert.Contains("X-Client-Guid", result.ErrorMessage);
    }

    /// <summary>
    /// 자동 회전 설정 불완전 시 키 생성 요청 (실패 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyGeneration")]
    [Trait("Category", "Validation")]
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

        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("2002", result.ErrorCode);
    }

    #endregion

    #region 키 조회 테스트

    /// <summary>
    /// 활성 키 조회 테스트 (정상 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyRetrieval")]
    public async Task GetKey_WithValidGuid_ReturnsSuccessOrNotFound()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        httpRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");

        // 키가 존재하면 200, 없으면 404
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound);

        var result = JsonSerializer.Deserialize<KmsResponse<string>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("0000", result.ErrorCode);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
        }
        else
        {
            Assert.Equal("2001", result.ErrorCode);
        }
    }

    /// <summary>
    /// 이전 버전의 키 조회 테스트 (정상 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyRetrieval")]
    public async Task GetPreviousKey_WithValidGuid_ReturnsSuccessOrNotFound()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys/previous");
        httpRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");

        // 키가 존재하면 200, 없으면 404
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound);

        var result = JsonSerializer.Deserialize<KmsResponse<string>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("0000", result.ErrorCode);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
        }
        else
        {
            Assert.Equal("2001", result.ErrorCode);
        }
    }

    /// <summary>
    /// X-Client-Guid 헤더 없이 키 조회 요청 (실패 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyRetrieval")]
    [Trait("Category", "Validation")]
    public async Task GetKey_WithoutHeader_ReturnsBadRequest()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        // X-Client-Guid 헤더를 추가하지 않음

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("9999", result.ErrorCode);
        Assert.Contains("X-Client-Guid", result.ErrorMessage);
    }

    /// <summary>
    /// 잘못된 GUID로 키 조회 요청 (실패 케이스)
    /// </summary>
    [Fact]
    [Trait("Category", "KeyRetrieval")]
    [Trait("Category", "Validation")]
    public async Task GetKey_WithInvalidGuid_ReturnsNotFoundOrForbidden()
    {
        // Arrange
        var invalidGuid = Guid.NewGuid(); // 존재하지 않는 GUID
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        httpRequest.Headers.Add("X-Client-Guid", invalidGuid.ToString());

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Content: {content}");
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Forbidden);

        var result = JsonSerializer.Deserialize<KmsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.ErrorCode == "1001" || result.ErrorCode == "1002");
    }

    #endregion

    #region 통합 시나리오 테스트

    /// <summary>
    /// 전체 플로우 테스트: 키 생성 → 키 조회
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
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

        var generateResponse = await _httpClient.SendAsync(httpRequest);
        var generateContent = await generateResponse.Content.ReadAsStringAsync();

        _output.WriteLine($"Generate Response Content: {generateContent}");
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);

        var generateResult = JsonSerializer.Deserialize<KmsResponse<EncryptionKeyEntity>>(generateContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(generateResult?.Data);
        var keyId = generateResult.Data.KeyId;

        // Step 2: 생성된 키 조회
        await Task.Delay(1000); // 1초 대기 (DB 반영 시간)

        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/keys");
        getRequest.Headers.Add("X-Client-Guid", _testClientGuid.ToString());

        var getResponse = await _httpClient.SendAsync(getRequest);
        var getContent = await getResponse.Content.ReadAsStringAsync();

        _output.WriteLine($"Get Response Content: {getContent}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getResult = JsonSerializer.Deserialize<KmsResponse<string>>(getContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(getResult?.Data);
        Assert.NotEmpty(getResult.Data);
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