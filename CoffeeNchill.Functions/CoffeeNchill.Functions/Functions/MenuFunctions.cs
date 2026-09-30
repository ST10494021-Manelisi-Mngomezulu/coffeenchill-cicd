using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// HTTP-triggered functions that manage the digital menu (MenuItems Azure Table).
    /// Replaces CoffeeNChill's paper/chalkboard menu with an editable Table Storage entity.
    /// </summary>
    public class MenuFunctions
    {
        private const string TableName = "MenuItems";

        private readonly ILogger<MenuFunctions> _logger;
        private readonly TableClient _tableClient;

        public MenuFunctions(ILogger<MenuFunctions> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableClient = tableServiceClient.GetTableClient(TableName);
        }

        // POST /api/menu
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> CreateMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("CreateMenuItem triggered.");

            CreateMenuItemDto? dto;
            try
            {
                dto = await JsonSerializer.DeserializeAsync<CreateMenuItemDto>(
                    req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return await BadRequest(req, "Request body is not valid JSON.");
            }

            if (dto is null || string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Sku)
                || string.IsNullOrWhiteSpace(dto.Name))
            {
                return await BadRequest(req, "Category, Sku and Name are required fields.");
            }

            if (dto.Price < 0)
            {
                return await BadRequest(req, "Price cannot be negative.");
            }

            var entity = new MenuItem
            {
                PartitionKey = dto.Category.Trim(),
                RowKey = dto.Sku.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                Price = dto.Price,
                IsAvailable = dto.IsAvailable
            };

            try
            {
                await _tableClient.AddEntityAsync(entity);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
            {
                return await Respond(req, HttpStatusCode.Conflict,
                    $"A menu item with SKU '{entity.RowKey}' already exists in category '{entity.PartitionKey}'.");
            }

            return await Respond(req, HttpStatusCode.Created, entity);
        }

        // GET /api/menu
        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> GetAllMenuItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("GetAllMenuItems triggered.");

            var items = new List<MenuItem>();
            await foreach (MenuItem item in _tableClient.QueryAsync<MenuItem>())
            {
                items.Add(item);
            }

            return await Respond(req, HttpStatusCode.OK, items);
        }

        // GET /api/menu/category/{category}
        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> GetMenuItemsByCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category)
        {
            _logger.LogInformation("GetMenuItemsByCategory triggered for {Category}.", category);

            if (string.IsNullOrWhiteSpace(category))
            {
                return await BadRequest(req, "Category route parameter is required.");
            }

            var items = new List<MenuItem>();
            await foreach (MenuItem item in _tableClient.QueryAsync<MenuItem>(x => x.PartitionKey == category))
            {
                items.Add(item);
            }

            return await Respond(req, HttpStatusCode.OK, items);
        }

        // PUT /api/menu/{category}/{id}
        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> UpdateMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            _logger.LogInformation("UpdateMenuItem triggered for {Category}/{Id}.", category, id);

            UpdateMenuItemDto? dto;
            try
            {
                dto = await JsonSerializer.DeserializeAsync<UpdateMenuItemDto>(
                    req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return await BadRequest(req, "Request body is not valid JSON.");
            }

            if (dto is null)
            {
                return await BadRequest(req, "Request body is required.");
            }

            MenuItem existing;
            try
            {
                existing = await _tableClient.GetEntityAsync<MenuItem>(category, id);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                return await Respond(req, HttpStatusCode.NotFound,
                    $"Menu item '{id}' in category '{category}' was not found.");
            }

            if (dto.Name is not null) existing.Name = dto.Name;
            if (dto.Description is not null) existing.Description = dto.Description;
            if (dto.Price is not null)
            {
                if (dto.Price < 0) return await BadRequest(req, "Price cannot be negative.");
                existing.Price = dto.Price.Value;
            }
            if (dto.IsAvailable is not null) existing.IsAvailable = dto.IsAvailable.Value;

            await _tableClient.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);

            return await Respond(req, HttpStatusCode.OK, existing);
        }

        // DELETE /api/menu/{category}/{id}
        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> DeleteMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            _logger.LogInformation("DeleteMenuItem triggered for {Category}/{Id}.", category, id);

            try
            {
                await _tableClient.DeleteEntityAsync(category, id);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                return await Respond(req, HttpStatusCode.NotFound,
                    $"Menu item '{id}' in category '{category}' was not found.");
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        // --- Small helpers to keep responses consistent ---

        private static async Task<HttpResponseData> Respond(HttpRequestData req, HttpStatusCode status, object body)
        {
            HttpResponseData response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(body, status);
            return response;
        }

        private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        {
            return await Respond(req, HttpStatusCode.BadRequest, new { error = message });
        }
    }
}
