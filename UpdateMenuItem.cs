using System;
using System.IO;
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
    /// Updates price and/or availability of a menu item.
    /// </summary>
    public class UpdateMenuItem
    {
        private readonly TableStorageService _tableService;

        public UpdateMenuItem(TableStorageService tableService)
        {
            _tableService = tableService;
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category,
            string id,
            FunctionContext context)
        {
            var logger = context.GetLogger("UpdateMenuItem");
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var doc = JsonDocument.Parse(body);

                double? price = null;
                bool? isAvailable = null;

                if (doc.RootElement.TryGetProperty("price", out var pEl) && pEl.ValueKind == JsonValueKind.Number)
                {
                    price = pEl.GetDouble();
                }

                if (doc.RootElement.TryGetProperty("isAvailable", out var aEl) && (aEl.ValueKind == JsonValueKind.True || aEl.ValueKind == JsonValueKind.False))
                {
                    isAvailable = aEl.GetBoolean();
                }

                if (!price.HasValue && !isAvailable.HasValue)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("No updatable fields provided. Provide price and/or isAvailable.");
                    return bad;
                }

                await _tableService.UpdateMenuItemAsync(category, id, price, isAvailable);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteStringAsync("Updated");
                return resp;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogInformation(ex, "Not found");
                var resp = req.CreateResponse(HttpStatusCode.NotFound);
                await resp.WriteStringAsync("Not Found");
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating menu item");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
