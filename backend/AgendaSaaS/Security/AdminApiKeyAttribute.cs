using Microsoft.AspNetCore.Mvc.Filters;

namespace AgendaSaaS.Security;

[Obsolete(
    "AdminApiKey quedó sin uso temporalmente. " +
    "Se mantiene como no-op hasta implementar login/JWT.")]
public class AdminApiKeyAttribute
    : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        await next();
    }
}