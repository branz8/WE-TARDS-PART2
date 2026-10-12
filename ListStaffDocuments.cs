using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class ListStaffDocuments
    {
        private readonly BlobContainerClient _container;

        public ListStaffDocuments(BlobContainerClient container)
        {
            _container = container;
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ListStaffDocuments");

            try
            {
                await _container.CreateIfNotExistsAsync();

                var documents = new List<object>();
                await foreach (var blobItem in _container.GetBlobsAsync())
                {
                    var blobClient = _container.GetBlobClient(blobItem.Name);
                    var props = await blobClient.GetPropertiesAsync();
                    documents.Add(new
                    {
                        fileName = blobItem.Name,
                        fileSize = props.Value.ContentLength,
                        lastModified = props.Value.LastModified,
                        fileType = props.Value.ContentType ?? "application/octet-stream"
                    });
                }

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(new { count = documents.Count, documents = documents });
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing documents");
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Internal Server Error");
                return err;
            }
        }
    }
}