using System;
using System.Text.Json.Serialization;

namespace CoffeeNChill.Functions
{
    /// <summary>
    /// Represents a menu item stored in Azure Table Storage.
    /// PartitionKey = Category, RowKey = SKU/ID
    /// </summary>
    public class MenuItem
    {
        /// <summary>
        /// Category used as PartitionKey (e.g., "Hot Drinks").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// SKU or unique identifier used as RowKey (e.g., "COF-001").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the menu item.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the menu item.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Price in the store currency.
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Whether the item is currently available.
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}
