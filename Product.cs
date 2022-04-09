using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakNSave
{
    public class Product
    {
        public String Id {get; private set;}
        public String Name {get; private set;}
        public String Subname { get; private set; }
        public double Price { get; private set; }
        public String ImageUrl { get; private set; }

        public Product(string id= "", string name = "", string subname = "", string imageUrl = "", double price=0.00)
        {
            Id = id;
            Name = name;
            Subname = subname;
            ImageUrl = imageUrl;
            Price = price;
        }
    }



    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Result
    {
        public string Id { get; set; }
        public string Language { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public string Html { get; set; }
    }

    public class SearchProduct
    {
        public int TotalTime { get; set; }
        public int CountTime { get; set; }
        public int QueryTime { get; set; }
        public string Signature { get; set; }
        public int Count { get; set; }
        public List<Result> Results { get; set; }
    }

    public class SearchBoxInfoData
    {
        public string endpoint { get; set; }
        public string suggestionEndpoint { get; set; }
        public string suggestionsMode { get; set; }
        public string resultPage { get; set; }
        public string targetSignature { get; set; }
        public string v { get; set; }
        public string s { get; set; }
        public int p { get; set; }
        public string searchResultsSignature { get; set; }
        public string itemid { get; set; }
    }


}
