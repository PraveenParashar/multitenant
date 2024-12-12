using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using MultiTenantApp.Services;
using MultiTenantApp.Models;
using System.Threading.Tasks;

namespace MultiTenantApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public ProjectController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet]
        [Route("getProjects")]
        [Authorize]
        public async Task<IActionResult> GetProjects()
        {
            var tenantId = HttpContext.Session.GetString("TenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(new { Message = "Tenant ID is missing." });
            }

            // Retrieve tenant details from the default database
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("DefaultDatabase");
            var tenantDetailsCollection = database.GetCollection<Models.TenantDetail>("TenantDetails");
            var tenantDetail = await tenantDetailsCollection.Find(t => t.TenantId == tenantId).FirstOrDefaultAsync();

            if (tenantDetail == null)
            {
                return NotFound(new { Message = "Tenant details not found." });
            }

            // Use tenant-specific connection string to connect to the database
            var tenantClient = new MongoClient(tenantDetail.ConnectionString);
            var tenantDatabase = tenantClient.GetDatabase(tenantDetail.TenantName);
            var collection = tenantDatabase.GetCollection<Models.Project>("Projects");

            var projects = await collection.Find(_ => true).ToListAsync();

            return Ok(projects);
        }
    }
}
