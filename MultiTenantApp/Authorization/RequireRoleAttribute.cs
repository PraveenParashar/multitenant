using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MultiTenantApp.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireRoleAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _requiredRoles;

        public RequireRoleAttribute(params string[] roles)
        {
            _requiredRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userRole = user.FindFirst("roles")?.Value;
            if (string.IsNullOrEmpty(userRole) || !_requiredRoles.Contains(userRole))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
