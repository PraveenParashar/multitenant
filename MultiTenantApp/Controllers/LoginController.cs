using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;

namespace MultiTenantApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        private readonly UserController _userController;

        public LoginController(ILogger<LoginController> logger, UserController userController)
        {
            _logger = logger;
            _userController = userController;
        }

        [HttpPost]
        [Route("login")]
        [Authorize]
        public async Task<IActionResult> Login()
        {
            try
            {
                // Register/update user in the database
                var userResult = await _userController.RegisterUser();
                if (userResult is ObjectResult objectResult && objectResult.StatusCode != 200)
                {
                    _logger.LogError($"Failed to register user: {objectResult.Value}");
                }

                // Extract user information from the token
                var userName = User.Identity?.Name;
                var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                var userRole = User.FindFirst("roles")?.Value;

                if (string.IsNullOrEmpty(userName))
                {
                    userName = User.FindFirst("preferred_username")?.Value ?? 
                              User.FindFirst("email")?.Value ?? 
                              User.FindFirst(ClaimTypes.Name)?.Value ?? 
                              "Unknown User";
                }

                // Set session context with tenant and user details
                HttpContext.Session.SetString("TenantId", tenantId ?? "default");
                HttpContext.Session.SetString("Username", userName);
                HttpContext.Session.SetString("Role", userRole ?? "User");

                return Ok(new { 
                    Message = "Login successful", 
                    TenantId = tenantId, 
                    Username = userName, 
                    Role = userRole,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login failed: {ex.Message}");
                return StatusCode(500, new { Message = "Login failed", Error = ex.Message });
            }
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string TenantId { get; set; }
    }
}
