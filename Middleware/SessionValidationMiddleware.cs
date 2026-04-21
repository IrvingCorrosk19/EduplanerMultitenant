using Microsoft.AspNetCore.Http;
using SchoolManager.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace SchoolManager.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
        {
            // Permitir todas las rutas bajo /Auth (sin importar mayúsculas/minúsculas)
            if (context.Request.Path.StartsWithSegments("/Auth", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var isAuthenticated = await currentUserService.IsAuthenticatedAsync();
            Console.WriteLine($"[Middleware] ¿Autenticado?: {isAuthenticated}");

            var user = await currentUserService.GetCurrentUserAsync();
            Console.WriteLine($"[Middleware] Usuario: {(user != null ? user.Email : "null")}");

            if (!isAuthenticated)
            {
                context.Response.Redirect("/Auth/Login");
                return;
            }

            if (user == null || user.Status?.ToLower() != "active")
            {
                await context.SignOutAsync();
                context.Response.Redirect("/Auth/Login");
                return;
            }

            if (user.SchoolId.HasValue)
            {
                var school = await currentUserService.GetCurrentUserSchoolAsync();
                if (school != null && !school.IsActive)
                {
                    await context.SignOutAsync();
                    context.Response.Redirect("/Auth/Login?schoolInactive=1");
                    return;
                }
            }

            await _next(context);
        }
    }
} 