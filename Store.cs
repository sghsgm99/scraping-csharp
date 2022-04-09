using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakNSave
{
    class Store : PakNSaveStore
    {
        public String Address { get; set; }
        public String Name { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    class PakNSaveStore
    {
        public string id { get; set; }
        public string name { get; set; }
        public string storeId { get; set; }
        public string EcomStoreId { get; set; }
        public bool NotSameStoreAsInEcom { get; set; }
        public string address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string openingHours { get; set; }
        public string url { get; set; }
        public string regionName { get; set; }
        public string regionCode { get; set; }
        public List<object> holidays { get; set; }
        public bool isCateringAvailable { get; set; }
    }



    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class OpeningHour
    {
        public string day { get; set; }
        public string dayShort { get; set; }
        public string open { get; set; }
        public string close { get; set; }
    }

    public class StoreDetails
    {
        public string id { get; set; }
        public string name { get; set; }
        public string banner { get; set; }
        public string address { get; set; }
        public bool clickAndCollect { get; set; }
        public bool delivery { get; set; }
        public bool boatDelivery { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string region { get; set; }
        public List<OpeningHour> openingHours { get; set; }
        public DateTime openingDate { get; set; }
        public bool onboardingMode { get; set; }
        public bool changeStorePopup { get; set; }
        public bool googleAdChangeStorePopup { get; set; }
    }

    public class StoreData
    {
        public bool success { get; set; }
        public StoreDetails storeDetails { get; set; }
        public bool deliveryByBoat { get; set; }


        
    }

}
