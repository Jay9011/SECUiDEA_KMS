using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SECUiDEA_KMS.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LocalhostOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
        var localIp = context.HttpContext.Connection.LocalIpAddress;

        if (!IsLocalRequest(remoteIp, localIp))
        {
            context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
            return;
        }
    }

    private bool IsLocalRequest(IPAddress? remoteIp, IPAddress? localIp)
    {
        if (remoteIp == null || localIp == null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(remoteIp) || remoteIp.Equals(localIp))
        {
            return true;
        }

        return false;
    }
}
