using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Retrieves menu items filtered by category (PartitionKey).
    /// </summary>
    public class GetMenuItemsByCategory
    {
        private readonly TableStorageService _tableService;

        public GetMenuItemsByCategory(TableStorageService tableService)
        {
            _tableService = tableService;
        }

        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetMenuItemsByCategory");
            try
            {
                var items = await _tableService.GetMenuItemsByCategoryAsync(category);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(items.Select(e => new
                {
                    Category = e.PartitionKey,
                    Id = e.RowKey,
                    Name = e.GetString("Name"),
                    Description = e.GetString("Description"),
                    Price = e.GetDouble("Price"),
                    IsAvailable = e.GetBoolean("IsAvailable")
                }));
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving menu items by category");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
