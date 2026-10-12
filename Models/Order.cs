using System;
using System.Collections.Generic;

namespace CoffeeNChill.Functions
{
    /// <summary>
    /// Order representation for queueing and table storage.
    /// </summary>
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<string> SelectedItemSKUs { get; set; } = new List<string>();
        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
    }
}
