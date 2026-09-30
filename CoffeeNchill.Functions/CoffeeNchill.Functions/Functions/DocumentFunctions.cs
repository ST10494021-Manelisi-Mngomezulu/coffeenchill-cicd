using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// HTTP-triggered functions that manage CoffeeNChill's operational documents
    /// (barista recipe sheets, cleaning manuals, health & safety policies) stored in the
    /// "staff-docs" Azure File Share. Uses the ASP.NET Core integration model
    /// (HttpRequest / IActionResult) so multipart/form-data uploads are handled for us.
    /// </summary>
    public class DocumentFunctions
    {
        // Only allow document-type uploads for the staff-docs share.
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".png", ".jpg", ".jpeg"
        };

        private readonly ILogger<DocumentFunctions> _logger;
        private readonly ShareClient _shareClient;

        public DocumentFunctions(ILogger<DocumentFunctions> logger, ShareClient shareClient)
        {
            _logger = logger;
            _shareClient = shareClient;
        }

        // POST /api/documents/upload
        [Function("UploadStaffDocument")]
        public async Task<IActionResult> UploadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequest req)
        {
            _logger.LogInformation("UploadStaffDocument triggered.");

            if (!req.HasFormContentType || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "Upload must be multipart/form-data with a 'file' field." });
            }

            IFormFile file = req.Form.Files[0];

            if (file.Length == 0)
            {
                return new BadRequestObjectResult(new { error = "Uploaded file is empty." });
            }

            string extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                return new BadRequestObjectResult(new
                {
                    error = $"File type '{extension}' is not allowed.",
                    allowed = AllowedExtensions
                });
            }

            ShareDirectoryClient rootDirectory = _shareClient.GetRootDirectoryClient();
            ShareFileClient fileClient = rootDirectory.GetFileClient(file.FileName);

            await using (Stream stream = file.OpenReadStream())
            {
                await fileClient.CreateAsync(file.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, file.Length), stream);
            }

            return new OkObjectResult(new
            {
                fileName = file.FileName,
                sizeBytes = file.Length,
                uploadedAt = DateTimeOffset.UtcNow
            });
        }

        // GET /api/documents
        [Function("ListStaffDocuments")]
        public async Task<IActionResult> ListStaffDocuments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequest req)
        {
            _logger.LogInformation("ListStaffDocuments triggered.");

            ShareDirectoryClient rootDirectory = _shareClient.GetRootDirectoryClient();
            var files = new List<object>();

            await foreach (ShareFileItem item in rootDirectory.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory) continue;

                ShareFileClient fileClient = rootDirectory.GetFileClient(item.Name);
                ShareFileProperties props = await fileClient.GetPropertiesAsync();

                files.Add(new
                {
                    fileName = item.Name,
                    sizeBytes = props.ContentLength,
                    lastModified = props.LastModified
                });
            }

            return new OkObjectResult(files);
        }

        // GET /api/documents/download/{fileName}
        [Function("DownloadStaffDocument")]
        public async Task<IActionResult> DownloadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequest req,
            string fileName)
        {
            _logger.LogInformation("DownloadStaffDocument triggered for {FileName}.", fileName);

            ShareDirectoryClient rootDirectory = _shareClient.GetRootDirectoryClient();
            ShareFileClient fileClient = rootDirectory.GetFileClient(fileName);

            bool exists = await fileClient.ExistsAsync();
            if (!exists)
            {
                return new NotFoundObjectResult(new { error = $"File '{fileName}' was not found in staff-docs." });
            }

            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            string contentType = download.ContentType is { Length: > 0 } ? download.ContentType : "application/octet-stream";

            return new FileStreamResult(download.Content, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }
}
