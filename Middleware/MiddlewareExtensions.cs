using Microsoft.AspNetCore.Builder;

namespace SchoolManager.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionValidationMiddleware>();
        }
    }
} 