using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class UploadStaffDocument
    {
        private readonly BlobContainerClient _container;

        public UploadStaffDocument(BlobContainerClient container)
        {
            _container = container;
        }

        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("UploadStaffDocument");

            try
            {
                await _container.CreateIfNotExistsAsync();

                string contentType = req.Headers.TryGetValues("Content-Type", out var cts)
                    ? string.Join(";", cts) : string.Empty;

                if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Expected multipart/form-data.");
                    return bad;
                }

                // Read raw body
                using var ms = new MemoryStream();
                await req.Body.CopyToAsync(ms);
                var bodyBytes = ms.ToArray();

                // Extract boundary
                var idx = contentType.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Multipart boundary missing.");
                    return bad;
                }
                var boundary = contentType.Substring(idx + "boundary=".Length).Trim('"', ' ');

                // Parse multipart
                var (fileName, fileBytes) = ExtractFile(bodyBytes, boundary);

                if (string.IsNullOrEmpty(fileName) || fileBytes == null || fileBytes.Length == 0)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("No file uploaded. Include a 'file' field in multipart/form-data.");
                    return bad;
                }

                var blobClient = _container.GetBlobClient(fileName);
                using (var stream = new MemoryStream(fileBytes))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(new
                {
                    message = $"File '{fileName}' uploaded successfully",
                    fileName = fileName,
                    fileSize = fileBytes.Length,
                    uploadTime = DateTime.UtcNow
                });
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading file");
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Error: " + ex.Message);
                return err;
            }
        }

        private static (string fileName, byte[] fileBytes) ExtractFile(byte[] body, string boundary)
        {
            var boundaryMarker = "--" + boundary;
            var boundaryBytes = Encoding.UTF8.GetBytes(boundaryMarker);
            var headerSepBytes = Encoding.UTF8.GetBytes("\r\n\r\n");

            int start = FindBytes(body, boundaryBytes, 0);
            if (start < 0) return (null, null);
            start += boundaryBytes.Length;

            int headerEnd = FindBytes(body, headerSepBytes, start);
            if (headerEnd < 0) return (null, null);

            var headers = Encoding.UTF8.GetString(body, start, headerEnd - start);
            int fileStart = headerEnd + 4;

            int nextBoundary = FindBytes(body, boundaryBytes, fileStart);
            if (nextBoundary < 0) nextBoundary = body.Length;

            int fileLen = nextBoundary - fileStart - 2;
            if (fileLen <= 0) return (null, null);

            var fileBytes = new byte[fileLen];
            Array.Copy(body, fileStart, fileBytes, 0, fileLen);

            var fileName = "uploaded-file";
            var match = System.Text.RegularExpressions.Regex.Match(headers, @"filename=""([^""]+)""");
            if (match.Success) fileName = match.Groups[1].Value;

            return (fileName, fileBytes);
        }

        private static int FindBytes(byte[] haystack, byte[] needle, int start)
        {
            for (int i = start; i <= haystack.Length - needle.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }
    }
}