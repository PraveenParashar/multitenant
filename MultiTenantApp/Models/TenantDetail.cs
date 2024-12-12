using System.ComponentModel.DataAnnotations;

namespace MultiTenantApp.Models
{
    public class TenantDetail
    {
        [Required]
        public string TenantId { get; set; }
        
        [Required]
        public string TenantName { get; set; }
        
        public string? ConnectionString { get; set; }
    }
}
