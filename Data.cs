using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakNSave
{
    public class Data
    {

    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class SaleType
    {
        public int minUnit { get; set; }
        public string type { get; set; }
        public int stepSize { get; set; }
        public string unit { get; set; }
    }

    public class ProductData
    {
        public string productId { get; set; }
        public double quantity { get; set; }
        public string sale_type { get; set; }
        public string name { get; set; }
        public int price { get; set; }
        public int catalogPrice { get; set; }
        public bool hasBadge { get; set; }
        public string badgeImageUrl { get; set; }
        public string imageUrl { get; set; }
        public bool restricted { get; set; }
        public List<SaleType> saleTypes { get; set; }
        public string weightDisplayName { get; set; }
        public string brand { get; set; }
        public string categoryName { get; set; }
        public string promoBadgeImageTitle { get; set; }
        public string uom { get; set; }
    }

    public class StoreProductData
    {
        public string storeId { get; set; }
        public string storeName { get; set; }
        public string storeAddress { get; set; }
    }

    public class CartData
    {
        public List<ProductData> products { get; set; }
        public List<object> unavailableProducts { get; set; }
        public double subtotal { get; set; }
        public double promoCodeDiscount { get; set; }
        public double saving { get; set; }
        public double serviceFee { get; set; }
        public double bagFee { get; set; }
        public StoreProductData store { get; set; }
        public int orderNumber { get; set; }
        public bool allowSubstitutions { get; set; }
        public bool wasRepriced { get; set; }
    }


}
