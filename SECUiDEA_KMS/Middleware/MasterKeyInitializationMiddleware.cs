using System.Runtime.Versioning;
using SECUiDEA_KMS.Services;

namespace SECUiDEA_KMS.Middleware;

/// <summary>
/// 마스터 키 초기화 확인 미들웨어
/// 상태에 따라 적절한 Setup 단계로 리다이렉트
/// </summary>
[SupportedOSPlatform("windows")]
public class MasterKeyInitializationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MasterKeyInitializationMiddleware> _logger;

    // Setup 관련 경로는 항상 허용 (API 및 실제 페이지)
    private readonly string[] _allowedPaths =
    {
        "/setup/step1",
        "/setup/step2",
        "/setup/completed",
        "/setup/createmasterkey",
        "/setup/recoverfrombackup",
        "/setup/testdatabaseconnection",
        "/setup/savedatabasesettings",
        "/setup/getstatus",
        "/setup/downloadbackupkey"
    };

    // Setup 진입점 경로 (/setup, /setup/index)
    private readonly string[] _setupEntryPaths =
    {
        "/setup",
        "/setup/index"
    };

    // 정적 리소스는 항상 허용
    private readonly string[] _allowedExtensions =
    {
        ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot"
    };

    public MasterKeyInitializationMiddleware(
        RequestDelegate next,
        ILogger<MasterKeyInitializationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        MasterKeyService masterKeyService,
        DatabaseSetupService databaseSetupService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var extension = Path.GetExtension(path);

        // 1. 정적 리소스는 항상 허용
        if (_allowedExtensions.Contains(extension))
        {
            await _next(context);
            return;
        }

        // 2. Setup 실제 페이지 및 API 경로는 항상 허용
        if (_allowedPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        // 3. Setup 진입점 경로 (/setup, /setup/index) - 상태에 따라 리다이렉트
        if (_setupEntryPaths.Any(p => path == p || path.StartsWith(p + "?")))
        {
            var redirectUrl = DetermineSetupStep(masterKeyService, databaseSetupService);

            _logger.LogInformation("Setup 진입점 접근. 리다이렉트: {RedirectUrl}, 요청 경로: {Path}", redirectUrl, path);

            context.Response.Redirect(redirectUrl);
            return;
        }

        // 4. 다른 경로 - 마스터 키 및 DB 설정 확인
        if (!masterKeyService.IsInitialized)
        {
            _logger.LogWarning("마스터 키가 초기화되지 않았습니다. Step1으로 리다이렉트합니다. 요청 경로: {Path}", path);

            // AJAX 요청인 경우 JSON 응답
            if (IsAjaxRequest(context.Request))
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\": false, \"message\": \"마스터 키가 초기화되지 않았습니다. Setup 페이지로 이동하세요.\", \"redirectUrl\": \"/setup/step1\"}");
                return;
            }

            context.Response.Redirect("/setup/step1");
            return;
        }

        if (!databaseSetupService.IsDatabaseConfigured())
        {
            _logger.LogWarning("데이터베이스가 설정되지 않았습니다. Step2로 리다이렉트합니다. 요청 경로: {Path}", path);

            // AJAX 요청인 경우 JSON 응답
            if (IsAjaxRequest(context.Request))
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\": false, \"message\": \"데이터베이스가 설정되지 않았습니다. Setup 페이지로 이동하세요.\", \"redirectUrl\": \"/setup/step2\"}");
                return;
            }

            context.Response.Redirect("/setup/step2");
            return;
        }

        // 5. 모든 설정이 완료되면 정상 처리
        await _next(context);
    }

    /// <summary>
    /// 현재 상태에 따라 적절한 Setup 단계 결정
    /// </summary>
    private string DetermineSetupStep(MasterKeyService masterKeyService, DatabaseSetupService databaseSetupService)
    {
        // 1. 마스터 키가 없으면 Step1
        if (!masterKeyService.IsInitialized)
        {
            return "/setup/step1";
        }

        // 2. 마스터 키는 있지만 DB 설정이 없으면 Step2
        if (!databaseSetupService.IsDatabaseConfigured())
        {
            return "/setup/step2";
        }

        // 3. 모두 완료되었으면 Completed
        return "/setup/completed";
    }

    /// <summary>
    /// AJAX 요청인지 확인
    /// </summary>
    private bool IsAjaxRequest(HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               request.Headers["Accept"].ToString().Contains("application/json");
    }
}

/// <summary>
/// Middleware 등록을 위한 확장 메서드
/// </summary>
public static class MasterKeyInitializationMiddlewareExtensions
{
    [SupportedOSPlatform("windows")]
    public static IApplicationBuilder UseMasterKeyInitializationCheck(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MasterKeyInitializationMiddleware>();
    }
}

