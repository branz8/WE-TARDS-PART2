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
    /// Retrieves all menu items from the MenuItems table.
    /// </summary>
    public class GetAllMenuItems
    {
        private readonly TableStorageService _tableService;

        public GetAllMenuItems(TableStorageService tableService)
        {
            _tableService = tableService;
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetAllMenuItems");
            try
            {
                var items = await _tableService.GetAllMenuItemsAsync();
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
                logger.LogError(ex, "Error retrieving all menu items");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
