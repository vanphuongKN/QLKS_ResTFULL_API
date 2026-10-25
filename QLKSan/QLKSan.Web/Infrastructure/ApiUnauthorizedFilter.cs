using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QLKSan.Infrastructure
{
    // Khi API trả 401 (token hết hạn/không hợp lệ): đăng xuất khỏi Web và chuyển về trang đăng nhập
    public class ApiUnauthorizedFilter : IAsyncExceptionFilter
    {
        public async Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.Exception is ApiException { StatusCode: HttpStatusCode.Unauthorized })
            {
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                context.ExceptionHandled = true;
            }
        }
    }
}
