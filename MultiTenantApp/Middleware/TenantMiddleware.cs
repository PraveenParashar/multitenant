using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MultiTenantApp.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip tenant validation for tenant creation endpoint
            if (context.Request.Path.StartsWithSegments("/api/tenant/create"))
            {
                await _next(context);
                return;
            }

            // Extract tenant information from the request
            var tenantId = context.Request.Headers["X-Tenant-ID"];

            if (string.IsNullOrEmpty(tenantId))
            {
                context.Response.StatusCode = 400; // Bad Request
                await context.Response.WriteAsync("Tenant ID is missing.");
                return;
            }

            // Store tenant information in context
            context.Items["TenantId"] = tenantId;

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}
