

using Newtonsoft.Json;

namespace AzureFuncApp.RestApi.Models
{
    internal class ShoppingCartItem
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime Created { get; set; } = DateTime.Now;

        public string ItemName { get; set; } = default!;

        public bool Collected { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; } = default!;
    }

    internal class CreateShoppingCartItem
    {
        public string ItemName { get; set; } = default!;

        public string Category { get; set; } = default!;
    }

    internal class UpdateShoppingCartItem
    {

        public bool Collected { get; set; }
    }
}
