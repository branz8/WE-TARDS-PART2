using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class CreateMenuItem
    {
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req,
            FunctionContext context)
        {
            Console.WriteLine("FUNCTION STARTED");
            Console.Out.Flush();

            var logger = context.GetLogger("CreateMenuItem");

            try
            {
                Console.WriteLine("Reading body");
                Console.Out.Flush();

                var body = await new StreamReader(req.Body).ReadToEndAsync();

                Console.WriteLine($"STEP 3: Body = {body} ");
                Console.Out.Flush();

                var item = JsonSerializer.Deserialize<MenuItem>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (item == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON");
                    return bad;
                }

                Console.WriteLine(" Deserialized OK ");
                Console.Out.Flush();

                var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "";
                Console.WriteLine($"STEP 5: Conn = {conn.Substring(0, Math.Min(50, conn.Length))}... !!!!!");
                Console.Out.Flush();

                var serviceClient = new TableServiceClient(conn);
                var tableClient = serviceClient.GetTableClient("MenuItems");

                Console.WriteLine(" Creating table if not exists ");
                Console.Out.Flush();

                await tableClient.CreateIfNotExistsAsync();

                Console.WriteLine("Table exists ");
                Console.Out.Flush();

                var entity = new TableEntity(item.Category, item.Id)
                {
                    { "Name", item.Name },
                    { "Description", item.Description },
                    { "Price", item.Price },
                    { "IsAvailable", item.IsAvailable }
                };

                Console.WriteLine("Adding entity");
                Console.Out.Flush();

                await tableClient.AddEntityAsync(entity);

                Console.WriteLine("Entity added");
                Console.Out.Flush();

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteStringAsync("Created");
                return resp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!!!! EXCEPTION: {ex.Message} !!!!!");
                Console.WriteLine($"!!!!! STACK: {ex.StackTrace} !!!!!");
                Console.Out.Flush();

                logger.LogError(ex, "Error creating menu item");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync($"Error: {ex.Message}");
                return resp;
            }
        }
    }
}