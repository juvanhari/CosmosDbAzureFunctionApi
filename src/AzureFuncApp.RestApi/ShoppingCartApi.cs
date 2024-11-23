using AzureFuncApp.RestApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzureFuncApp.RestApi
{
    public class ShoppingCartApi
    {
        private readonly ILogger<ShoppingCartApi> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container documentContainer;

        public ShoppingCartApi(ILogger<ShoppingCartApi> logger,IConfiguration configuration, CosmosClient client)
        {
            _logger = logger;
            _cosmosClient = client;
            documentContainer = _cosmosClient.GetContainer(configuration.GetValue<string>("CosmosDBDatabase"), "ShoppingCartItem");
        }

        [Function("GetShoppingCartItems")]
        public async Task<IActionResult> GetShoppingCartItems([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shoppingcartitem")] HttpRequest req)
        {
            await Task.CompletedTask;
            _logger.LogInformation("Received function request for sending all shopping cart items");

            List<ShoppingCartItem> shoppingCartItems = new();
            var items = documentContainer.GetItemLinqQueryable<ShoppingCartItem>().ToFeedIterator();
            while(items.HasMoreResults)
            {
                var response = await items.ReadNextAsync();
                shoppingCartItems.AddRange(response.ToList());
            }
            return new OkObjectResult(shoppingCartItems);
        }

        [Function("GetShoppingCartItemById")]
        public async Task<IActionResult> GetShoppingCartItemById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shoppingcartitem/{id}/{category}")] HttpRequest req,
            string id,string category)
        {
            await Task.CompletedTask;
            _logger.LogInformation($"Getting Shopping Cart with ID: {id}");
            try
            {
                var item = await documentContainer.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
                if (item.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new NotFoundResult();
                }
                return new OkObjectResult(item.Resource);
            }
            catch (CosmosException cex)
            {
                if (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new NotFoundResult();
                }
                return new BadRequestObjectResult(cex.Message); 
            }
        }

        [Function("CreateShoppingCartItem")]
        public async Task<IActionResult> CreateShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "shoppingcartitem")] HttpRequest req)
        {
            _logger.LogInformation("Creating a new shopping cart item");

            string requestData = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<CreateShoppingCartItem>(requestData);

            var item = new ShoppingCartItem()
            {
                ItemName = data!.ItemName,
                Category = data!.Category
            };
            var response = await documentContainer.CreateItemAsync(item, new PartitionKey(item.Category));
            return new OkObjectResult(response.Resource);
        }

        [Function("PutShoppingCartItem")]
        public async Task<IActionResult> PutShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "shoppingcartitem/{id}/{category}")] HttpRequest req,
            string id, string category)
        {
            _logger.LogInformation("Received function request for updating the item.");
           
            try
            {
                var existingItem = await documentContainer.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));

                string requestData = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<UpdateShoppingCartItem>(requestData);

                var item = new ShoppingCartItem()
                {
                    ItemName = existingItem.Resource!.ItemName,
                    Category = existingItem.Resource!.Category,
                    Collected = data.Collected,
                    Id = id
                };

                await documentContainer.ReplaceItemAsync(item, id, new PartitionKey(category));
                return new OkObjectResult(item);
            }
            catch (CosmosException cex)
            {
                if (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new NotFoundResult();
                }
                return new BadRequestObjectResult(cex.Message);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("DeleteShoppingCartItem")]
        public async Task<IActionResult> DeleteShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "shoppingcartitem/{id}/{category}")] HttpRequest req,
            string id,string category)
        {
            await Task.CompletedTask;
            _logger.LogInformation("Received function request for deleting the item.");
            await documentContainer.DeleteItemAsync<ShoppingCartItem>(id, new PartitionKey(category));

            return new OkResult();
        }
    }
}

