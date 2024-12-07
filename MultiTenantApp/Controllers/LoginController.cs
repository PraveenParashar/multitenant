using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using System.Threading.Tasks;

namespace MultiTenantApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        [Route("login")]
        [Authorize] // Require authorization
        public async Task<IActionResult> Login()
        {
            // Extract user information from the token
            var userName = User.Identity.Name;
            var tenantId = HttpContext.User.FindFirst("tid")?.Value;
            var userRole = HttpContext.User.FindFirst("roles")?.Value;

            // Set session context with tenant and user details
            HttpContext.Session.SetString("TenantId", tenantId);
            HttpContext.Session.SetString("Username", userName);
            HttpContext.Session.SetString("Role", userRole ?? "User");

            return Ok(new { Message = "Login successful", TenantId = tenantId, Username = userName, Role = userRole });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string TenantId { get; set; }
    }
}
