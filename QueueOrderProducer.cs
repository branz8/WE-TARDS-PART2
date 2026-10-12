using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions.Functions
{
    public class QueueOrderProducer
    {
        private readonly QueueClient _queueClient;

        public QueueOrderProducer(QueueClient queueClient)
        {
            _queueClient = queueClient;
        }

        [Function("QueueOrderProducer")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("QueueOrderProducer");

            try
            {
                await _queueClient.CreateIfNotExistsAsync();

                // Read body manually — no ReadFromJsonAsync
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Request body is empty.");
                    return bad;
                }

                // Validate JSON
                var order = JsonSerializer.Deserialize<Order>(body);
                if (order == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid order JSON.");
                    return bad;
                }

                // Base64-encode the JSON (Azure Queue requirement)
                var bytes = System.Text.Encoding.UTF8.GetBytes(body);
                var base64 = Convert.ToBase64String(bytes);

                await _queueClient.SendMessageAsync(base64);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(new
                {
                    message = "Order queued successfully",
                    orderId = order.OrderId,
                    queuedAt = DateTime.UtcNow
                });
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error queueing order");
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Error: " + ex.Message);
                return err;
            }
        }
    }
}