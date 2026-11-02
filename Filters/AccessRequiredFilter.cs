using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Filters;

public class AccessRequiredFilter : IAsyncActionFilter
{
    public const string SessionKey = "AccesoAutorizado";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var isAnonymous = context.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (isAnonymous ||
            context.HttpContext.Session.GetString(SessionKey) == "true")
        {
            await next();
            return;
        }

        var returnUrl = context.HttpContext.Request.Path +
            context.HttpContext.Request.QueryString;

        context.Result = new RedirectToActionResult(
            "Login",
            "Acceso",
            new { returnUrl = returnUrl.ToString() });
    }
}
