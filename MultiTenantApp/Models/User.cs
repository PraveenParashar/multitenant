using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace MultiTenantApp.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public string UserId { get; set; }  // Azure AD Object ID

        [Required]
        public string TenantId { get; set; }  // Azure AD Tenant ID

        [Required]
        public string Email { get; set; }

        [Required]
        public string DisplayName { get; set; }

        public string Role { get; set; }

        public string Department { get; set; }

        public string JobTitle { get; set; }

        public string PhoneNumber { get; set; }

        public string ProfilePictureUrl { get; set; }

        public string TimeZone { get; set; }

        public string Language { get; set; }

        public UserPreferences Preferences { get; set; } = new UserPreferences();

        public List<string> Permissions { get; set; } = new List<string>();

        public DateTime CreatedAt { get; set; }

        public DateTime LastLoginAt { get; set; }

        public DateTime? LastPasswordChangeAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsMfaEnabled { get; set; }

        public Dictionary<string, string> CustomAttributes { get; set; } = new Dictionary<string, string>();

        public string[] Teams { get; set; } = Array.Empty<string>();

        public AuditInfo AuditInfo { get; set; } = new AuditInfo();
    }

    public class UserPreferences
    {
        public string Theme { get; set; } = "light";
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public Dictionary<string, bool> FeatureFlags { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, string> UISettings { get; set; } = new Dictionary<string, string>();
    }

    public class AuditInfo
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public List<UserActivity> RecentActivity { get; set; } = new List<UserActivity>();
    }

    public class UserActivity
    {
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
