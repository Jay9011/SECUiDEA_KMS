using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace ApiTestProject;

/// <summary>
/// KMS API 테스트를 위한 공유 픽스처
/// 모든 테스트가 공유하는 HttpClient와 설정 제공
/// </summary>
public class KmsApiFixture : IDisposable
{
    public HttpClient HttpClient { get; }
    public string BaseUrl { get; }
    public Guid TestClientGuid { get; }

    public KmsApiFixture()
    {
        // 설정 파일 읽기
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        BaseUrl = configuration["KmsApiSettings:BaseUrl"] ?? "https://localhost:7443";
        var guidString = configuration["KmsApiSettings:TestClientGuid"];

        if (string.IsNullOrEmpty(guidString) || !Guid.TryParse(guidString, out var parsedGuid))
        {
            throw new InvalidOperationException(
                "appsettings.json에 유효한 TestClientGuid를 설정해야 합니다.");
        }

        TestClientGuid = parsedGuid;

        // HttpClient 초기화
        var handler = new HttpClientHandler
        {
            // 개발 환경에서 SSL 인증서 검증 무시 (프로덕션에서는 제거)
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        HttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void Dispose()
    {
        HttpClient?.Dispose();
    }
}

