using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class DownloadStaffDocument
    {
        private readonly BlobContainerClient _container;

        public DownloadStaffDocument(BlobContainerClient container)
        {
            _container = container;
        }

        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequestData req,
            string fileName,
            FunctionContext context)
        {
            var logger = context.GetLogger("DownloadStaffDocument");

            try
            {
                var blobClient = _container.GetBlobClient(fileName);
                if (!await blobClient.ExistsAsync())
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteStringAsync($"File '{fileName}' not found.");
                    return notFound;
                }

                var download = await blobClient.DownloadStreamingAsync();
                var contentType = "application/octet-stream";
                if (fileName.EndsWith(".pdf")) contentType = "application/pdf";
                else if (fileName.EndsWith(".txt")) contentType = "text/plain";
                else if (fileName.EndsWith(".docx")) contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

                var resp = req.CreateResponse(HttpStatusCode.OK);
                resp.Headers.Add("Content-Type", contentType);
                resp.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                await download.Value.Content.CopyToAsync(resp.Body);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading file");
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Internal Server Error");
                return err;
            }
        }
    }
}