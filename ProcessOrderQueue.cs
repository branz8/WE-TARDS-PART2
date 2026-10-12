using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Processes order messages from the order-processing-queue and records them in Orders table with status transitions.
    /// </summary>
    public class ProcessOrderQueue
    {
        private readonly TableStorageService _tableService;

        public ProcessOrderQueue(TableStorageService tableService)
        {
            _tableService = tableService;
        }

        [Function("ProcessOrderQueue")]
        public async Task Run([QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")] string base64Message, FunctionContext context)
        {
            var logger = context.GetLogger("ProcessOrderQueue");
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Message));
                var order = JsonSerializer.Deserialize<Order>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (order == null)
                {
                    logger.LogWarning("Received empty or invalid order message");
                    return;
                }

                var partition = order.OrderTimestamp.ToString("yyyy-MM-dd");
                var entity = new TableEntity(partition, order.OrderId)
                {
                    { "Status", "Received" },
                    { "CustomerName", order.CustomerName },
                    { "TotalPrice", order.TotalPrice },
                    { "OrderTimestamp", order.OrderTimestamp }
                };

                await _tableService.InsertOrMergeOrderEntityAsync(entity);
                logger.LogInformation("Order {OrderId} recorded as Received", order.OrderId);

                // Simulate status transitions
                var statuses = new[] { "Preparing", "Ready", "Collected" };
                foreach (var status in statuses)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    entity["Status"] = status;
                    await _tableService.InsertOrMergeOrderEntityAsync(entity);
                    logger.LogInformation("Order {OrderId} updated to {Status}", order.OrderId, status);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing order queue message");
            }
        }
    }
}
