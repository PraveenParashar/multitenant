using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MultiTenantApp.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MultiTenantApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly IMongoCollection<TenantDetail> _tenantDetailsCollection;
        private const string MongoDbAtlasConnectionString = "mongodb+srv://praveenparashar2021:mongodbtest2021@cluster0.gew8r.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";

        public TenantController(IServiceProvider serviceProvider)
        {
            var client = new MongoClient(MongoDbAtlasConnectionString);
            var database = client.GetDatabase("DefaultDatabase");
            _tenantDetailsCollection = database.GetCollection<TenantDetail>("TenantDetails");
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateTenant([FromBody] TenantDetail tenantDetail)
        {
            if (string.IsNullOrEmpty(tenantDetail?.TenantId))
            {
                return BadRequest(new { Message = "TenantId is required" });
            }

            // Set the tenant's database connection string
            tenantDetail.ConnectionString = MongoDbAtlasConnectionString;

            // Insert tenant details into the collection
            await _tenantDetailsCollection.InsertOneAsync(tenantDetail);

            return Ok(new { Message = "Tenant created successfully", TenantId = tenantDetail.TenantId });
        }
    }
}
