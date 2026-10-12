using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions
{
    /// <summary>
    /// Helper service for Azure Table Storage operations.
    /// Uses async methods throughout to avoid sync-over-async deadlocks.
    /// </summary>
    public class TableStorageService
    {
        private readonly TableServiceClient _serviceClient;

        public TableStorageService(string connectionString)
        {
            _serviceClient = new TableServiceClient(connectionString);
        }

        /// <summary>
        /// Gets (and creates if missing) a table client asynchronously.
        /// </summary>
        public async Task<TableClient> GetTableClientAsync(string tableName)
        {
            var client = _serviceClient.GetTableClient(tableName);
            await client.CreateIfNotExistsAsync();
            return client;
        }

        // === MENU ITEMS ===

        public async Task InsertMenuItemAsync(MenuItem item)
        {
            var client = await GetTableClientAsync("MenuItems");
            var entity = new TableEntity(item.Category, item.Id)
            {
                { "Name", item.Name },
                { "Description", item.Description },
                { "Price", item.Price },
                { "IsAvailable", item.IsAvailable }
            };
            await client.AddEntityAsync(entity);
        }

        public async Task<IEnumerable<TableEntity>> GetAllMenuItemsAsync()
        {
            var client = await GetTableClientAsync("MenuItems");
            var items = client.QueryAsync<TableEntity>();
            var list = new List<TableEntity>();
            await foreach (var e in items)
            {
                list.Add(e);
            }
            return list;
        }

        public async Task<IEnumerable<TableEntity>> GetMenuItemsByCategoryAsync(string category)
        {
            var client = await GetTableClientAsync("MenuItems");
            var items = client.QueryAsync<TableEntity>(ent => ent.PartitionKey == category);
            var list = new List<TableEntity>();
            await foreach (var e in items)
            {
                list.Add(e);
            }
            return list;
        }

        public async Task<TableEntity?> GetMenuItemAsync(string category, string id)
        {
            var client = await GetTableClientAsync("MenuItems");
            try
            {
                var resp = await client.GetEntityAsync<TableEntity>(category, id);
                return resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateMenuItemAsync(string category, string id, double? price, bool? isAvailable)
        {
            var client = await GetTableClientAsync("MenuItems");
            var existing = await GetMenuItemAsync(category, id);
            if (existing == null) throw new InvalidOperationException("Menu item not found");

            if (price.HasValue) existing["Price"] = price.Value;
            if (isAvailable.HasValue) existing["IsAvailable"] = isAvailable.Value;

            await client.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteMenuItemAsync(string category, string id)
        {
            var client = await GetTableClientAsync("MenuItems");
            await client.DeleteEntityAsync(category, id);
        }

        // === ORDERS ===

        public async Task EnsureOrdersTableAsync()
        {
            await GetTableClientAsync("Orders");
        }

        public async Task InsertOrMergeOrderEntityAsync(TableEntity entity)
        {
            var client = await GetTableClientAsync("Orders");
            await client.UpsertEntityAsync(entity, TableUpdateMode.Merge);
        }
    }
}