using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models
{
    /// <summary>
    /// Table Storage entity for the "MenuItems" table.
    /// PartitionKey = Category (e.g. "Hot Drinks"), RowKey = Item SKU/ID (e.g. "COF-001").
    /// </summary>
    public class MenuItem : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty; // Category
        public string RowKey { get; set; } = string.Empty;       // SKU / ID

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    /// <summary>DTO used for creating a new menu item via POST /api/menu.</summary>
    public class CreateMenuItemDto
    {
        public string Category { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    /// <summary>DTO used for updating price/availability via PUT /api/menu/{category}/{id}.</summary>
    public class UpdateMenuItemDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
