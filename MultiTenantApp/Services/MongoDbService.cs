using MongoDB.Driver;
using System;

namespace MultiTenantApp.Services
{
    public class MongoDbService
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;

        public MongoDbService(string tenantId)
        {
            var connectionString = GetConnectionStringForTenant(tenantId);
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(GetDatabaseNameForTenant(tenantId));
        }

        private string GetConnectionStringForTenant(string tenantId)
        {
            // Logic to construct the connection string based on the tenantId
            // This is a placeholder; replace with actual logic to fetch connection strings
            return $"mongodb://username:password@localhost:27017/{tenantId}";
        }

        private string GetDatabaseNameForTenant(string tenantId)
        {
            // Logic to determine the database name based on the tenantId
            // This is a placeholder; replace with actual logic
            return $"Tenant_{tenantId}";
        }

        // Example method to get a collection
        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }
    }
}
