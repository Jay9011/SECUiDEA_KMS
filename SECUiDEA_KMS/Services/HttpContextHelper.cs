namespace SECUiDEA_KMS.Services;

public class HttpContextHelper
{
    #region 의존 주입
    private readonly IHttpContextAccessor _httpContextAccessor;
    public HttpContextHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    #endregion

    protected HttpContext? context => _httpContextAccessor.HttpContext;
    protected HttpRequest? request => context?.Request;
    protected HttpResponse? response => context?.Response;

    public string? GetClientIpAddress()
    {
        if (context == null) return null;

        string? ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["X-Real-IP"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["HTTP_X_FORWARDED_FOR"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["HTTP_X_REAL_IP"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["REMOTE_ADDR"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["HTTP_CLIENT_IP"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["HTTP_X_CLUSTER_CLIENT_IP"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        ipAddress = request?.Headers["HTTP_FORWARDED_FOR"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        return null;
    }
}
