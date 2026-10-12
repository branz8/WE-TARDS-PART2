using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Deletes a menu item from the MenuItems table.
    /// </summary>
    public class DeleteMenuItem
    {
        private readonly TableStorageService _tableService;

        public DeleteMenuItem(TableStorageService tableService)
        {
            _tableService = tableService;
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category,
            string id,
            FunctionContext context)
        {
            var logger = context.GetLogger("DeleteMenuItem");
            try
            {
                await _tableService.DeleteMenuItemAsync(category, id);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteStringAsync("Deleted");
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting menu item");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
