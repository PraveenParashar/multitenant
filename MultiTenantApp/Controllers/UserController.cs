using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MultiTenantApp.Models;
using MultiTenantApp.Authorization;
using System.Security.Claims;

namespace MultiTenantApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly ILogger<UserController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserController(
            IConfiguration configuration, 
            ILogger<UserController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            var mongoClient = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = mongoClient.GetDatabase(configuration["MongoDB:DatabaseName"]);
            _userCollection = database.GetCollection<User>("Users");
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterUser()
        {
            try
            {
                var userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                var email = User.FindFirst("preferred_username")?.Value ?? 
                           User.FindFirst("email")?.Value ??
                           User.FindFirst(ClaimTypes.Email)?.Value;
                var name = User.FindFirst("name")?.Value ?? 
                          User.FindFirst(ClaimTypes.Name)?.Value ?? 
                          "Unknown User";
                var role = User.FindFirst("roles")?.Value ?? "User";

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Invalid user claims");
                }

                // Check if user already exists
                var existingUser = await _userCollection
                    .Find(u => u.UserId == userId && u.TenantId == tenantId)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    // Update user information and track activity
                    var activity = new UserActivity
                    {
                        ActivityType = "Login",
                        Description = "User logged in",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = GetClientIpAddress(),
                        UserAgent = GetUserAgent()
                    };

                    var update = Builders<User>.Update
                        .Set(u => u.LastLoginAt, DateTime.UtcNow)
                        .Set(u => u.Email, email)
                        .Set(u => u.DisplayName, name)
                        .Set(u => u.Role, role)
                        .Push(u => u.AuditInfo.RecentActivity, activity);

                    await _userCollection.UpdateOneAsync(
                        u => u.Id == existingUser.Id,
                        update
                    );

                    _logger.LogInformation($"User {userId} from tenant {tenantId} logged in");
                    return Ok(existingUser);
                }

                // Create new user with audit information
                var newUser = new User
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Email = email,
                    DisplayName = name,
                    Role = role,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true,
                    TimeZone = "UTC",
                    Language = "en-US",
                    AuditInfo = new AuditInfo
                    {
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        RecentActivity = new List<UserActivity>
                        {
                            new UserActivity
                            {
                                ActivityType = "Registration",
                                Description = "User registered",
                                Timestamp = DateTime.UtcNow,
                                IpAddress = GetClientIpAddress(),
                                UserAgent = GetUserAgent()
                            }
                        }
                    }
                };

                await _userCollection.InsertOneAsync(newUser);
                _logger.LogInformation($"New user {userId} from tenant {tenantId} registered");

                return Ok(newUser);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering user: {ex.Message}");
                return StatusCode(500, "Error registering user");
            }
        }

        [HttpGet]
        [Route("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Invalid user claims");
                }

                var user = await _userCollection
                    .Find(u => u.UserId == userId && u.TenantId == tenantId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound("User not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user: {ex.Message}");
                return StatusCode(500, "Error retrieving user information");
            }
        }

        [HttpPut]
        [Route("me/preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferences preferences)
        {
            try
            {
                var userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Invalid user claims");
                }

                var update = Builders<User>.Update
                    .Set(u => u.Preferences, preferences)
                    .Set(u => u.AuditInfo.LastModifiedAt, DateTime.UtcNow)
                    .Set(u => u.AuditInfo.LastModifiedBy, userId);

                var result = await _userCollection.UpdateOneAsync(
                    u => u.UserId == userId && u.TenantId == tenantId,
                    update
                );

                if (result.ModifiedCount == 0)
                {
                    return NotFound("User not found");
                }

                return Ok(preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating preferences: {ex.Message}");
                return StatusCode(500, "Error updating preferences");
            }
        }

        [HttpGet]
        [Route("tenant/{tenantId}")]
        [RequireRole("Admin")]
        public async Task<IActionResult> GetUsersByTenant(
            string tenantId,
            [FromQuery] string searchTerm = "",
            [FromQuery] string department = "",
            [FromQuery] bool activeOnly = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Ensure user has access to this tenant
                var userTenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                if (userTenantId != tenantId)
                {
                    return Forbid("You don't have access to this tenant");
                }

                var filterBuilder = Builders<User>.Filter;
                var filter = filterBuilder.Eq(u => u.TenantId, tenantId);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchFilter = filterBuilder.Or(
                        filterBuilder.Regex(u => u.DisplayName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                        filterBuilder.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
                    );
                    filter = filterBuilder.And(filter, searchFilter);
                }

                if (!string.IsNullOrEmpty(department))
                {
                    filter = filterBuilder.And(filter, filterBuilder.Eq(u => u.Department, department));
                }

                if (activeOnly)
                {
                    filter = filterBuilder.And(filter, filterBuilder.Eq(u => u.IsActive, true));
                }

                var users = await _userCollection
                    .Find(filter)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                var totalCount = await _userCollection.CountDocumentsAsync(filter);

                return Ok(new
                {
                    Users = users,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users for tenant {tenantId}: {ex.Message}");
                return StatusCode(500, "Error retrieving users");
            }
        }

        [HttpPut]
        [Route("{userId}/profile")]
        public async Task<IActionResult> UpdateProfile(string userId, [FromBody] UserProfileUpdate profile)
        {
            try
            {
                var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                var currentUserId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("Invalid user claims");
                }

                // Only allow users to update their own profile unless they're an admin
                if (userId != currentUserId && User.FindFirst("roles")?.Value != "Admin")
                {
                    return Forbid("You don't have permission to update this user's profile");
                }

                var update = Builders<User>.Update
                    .Set(u => u.DisplayName, profile.DisplayName)
                    .Set(u => u.Department, profile.Department)
                    .Set(u => u.JobTitle, profile.JobTitle)
                    .Set(u => u.PhoneNumber, profile.PhoneNumber)
                    .Set(u => u.TimeZone, profile.TimeZone)
                    .Set(u => u.Language, profile.Language)
                    .Set(u => u.AuditInfo.LastModifiedAt, DateTime.UtcNow)
                    .Set(u => u.AuditInfo.LastModifiedBy, currentUserId);

                var result = await _userCollection.UpdateOneAsync(
                    u => u.UserId == userId && u.TenantId == tenantId,
                    update
                );

                if (result.ModifiedCount == 0)
                {
                    return NotFound("User not found");
                }

                return Ok("Profile updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating profile for user {userId}: {ex.Message}");
                return StatusCode(500, "Error updating profile");
            }
        }

        [HttpPut]
        [Route("{userId}/deactivate")]
        [RequireRole("Admin")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            try
            {
                var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                var currentUserId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Invalid tenant");
                }

                var update = Builders<User>.Update
                    .Set(u => u.IsActive, false)
                    .Set(u => u.AuditInfo.LastModifiedAt, DateTime.UtcNow)
                    .Set(u => u.AuditInfo.LastModifiedBy, currentUserId)
                    .Push(u => u.AuditInfo.RecentActivity, new UserActivity
                    {
                        ActivityType = "AccountDeactivated",
                        Description = "Account was deactivated by administrator",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = GetClientIpAddress(),
                        UserAgent = GetUserAgent()
                    });

                var result = await _userCollection.UpdateOneAsync(
                    u => u.UserId == userId && u.TenantId == tenantId,
                    update
                );

                if (result.ModifiedCount == 0)
                {
                    return NotFound("User not found or already deactivated");
                }

                return Ok("User deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deactivating user {userId}: {ex.Message}");
                return StatusCode(500, "Error deactivating user");
            }
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }
    }

    public class UserProfileUpdate
    {
        public string DisplayName { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string TimeZone { get; set; }
        public string Language { get; set; }
    }
}
