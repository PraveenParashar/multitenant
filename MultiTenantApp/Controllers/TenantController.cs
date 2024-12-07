using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MultiTenantApp.Services;
using System.Threading.Tasks;

namespace MultiTenantApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly IMongoCollection<TenantDetail> _tenantDetailsCollection;

        public TenantController(MongoDbService mongoDbService)
        {
            // Connect to the default database and collection
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("DefaultDatabase");
            _tenantDetailsCollection = database.GetCollection<TenantDetail>("TenantDetails");
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateTenant([FromBody] TenantDetail tenantDetail)
        {
            // Insert tenant details into the collection
            await _tenantDetailsCollection.InsertOneAsync(tenantDetail);

            return Ok(new { Message = "Tenant created successfully", TenantId = tenantDetail.TenantId });
        }
    }

    public class TenantDetail
    {
        public string TenantId { get; set; }
        public string TenantName { get; set; }
        public string ConnectionString { get; set; }
    }
}
