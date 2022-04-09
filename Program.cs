using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PakNSave
{
    class Program
    {
        class Credentials
        {
            public string email { get;  set; }
            public string password { get; set; }

            internal Credentials(string email, string password)
            {
                this.email = email;
                this.password = password;
            }
        }

        class RequestProduct
        {
            public string productId { get; set; }
            public int quantity { get; set; }
            public string sale_type { get; set; }

            internal RequestProduct(string productId, int quantity)
            {
                this.productId = productId;
                this.quantity = quantity;
                this.sale_type = "UNITS";
            }
        }

        class RequestProducts
        {            
            public RequestProduct[] products { get; set; }

            internal RequestProducts(string productId, int quantity)
            {
                products = new RequestProduct[1];
                products[0] = new RequestProduct(productId, quantity);
            }
        }
      
        private StoreData SelectedStore { get; set; }
        private CookieContainer container = new CookieContainer();

        private const String UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        private string NavigationUrl = "https://www.paknsave.co.nz/CommonApi/Navigation/MegaMenu?v=22178&storeId=";

        // Storage Location info
        private string StoreId;
        private string UserLat;
        private string UserLng;
        private string StoreLat;
        private string StoreLng;
        private bool IsSuccess;

        private string __requestVerificationToken;

        private string XNewRelicID;

        private XmlDictionaryReaderQuotas myReaderQuotas = new XmlDictionaryReaderQuotas();
        public Program()
        {
            myReaderQuotas.MaxNameTableCharCount = 1024 * 1024 * 32;
        }



        private static string GetContent(HttpWebResponse response)
        {
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();

            return content;
        }
        private void FirstCall(ref List<Store> stores)
        {
            try
            {
                //https://www.paknsave.co.nz/BrandsApi/BrandsStore/GetBrandStores
                HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create("https://www.paknsave.co.nz/BrandsApi/BrandsStore/GetBrandStores");
                request1.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request1.ServicePoint.Expect100Continue = false;
                request1.AllowAutoRedirect = false;
                request1.UserAgent = UserAgent;
                request1.Headers.Add("Upgrade-Insecure-Requests", "1");
                request1.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request1.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request1.CookieContainer = container;

                using (HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse())
                {
                    container.Add(response1.Cookies);
                    string content = GetContent(response1);

                    var strs = JsonConvert.DeserializeObject<List<Store>>(content);

                    if(strs!=null && strs.Any()){
                        stores = new List<Store>(strs);

                        foreach(var st in stores)
                        {
                            Console.WriteLine(st.name);
                        }
                    }

                }
                //https://www.paknsave.co.nz/CommonApi/Store/ChangeStore?storeId=48dd98cc-a757-4102-b15d-ea82b4b10571 
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);                
            }
        }

        /// <summary>
        /// A method to call off to the grocery website and return a list of grocery categories
        /// </summary>
        public async Task<NavigationData> GetCategories(string storeId)
        {

            try
            {

                NavigationData navData = new NavigationData();
                var navigationUrlConcat = NavigationUrl + storeId;
                HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(navigationUrlConcat);
                request1.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request1.ServicePoint.Expect100Continue = false;
                request1.AllowAutoRedirect = false;
                request1.UserAgent = UserAgent;
                request1.Headers.Add("Upgrade-Insecure-Requests", "1");
                request1.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request1.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request1.CookieContainer = container;

                using (HttpWebResponse response1 = (HttpWebResponse) await request1.GetResponseAsync())
                {
                    container.Add(response1.Cookies);
                    string content = GetContent(response1);

                    var dt = JsonConvert.DeserializeObject<NavigationData>(content);
                    return dt;
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return new NavigationData();
        }

        private void AddProducts(HtmlDocument document, List<Product> result, uint number)
        {
            HtmlNodeCollection productNodes = document.DocumentNode.SelectNodes("//div[@class='fs-product-card']");
            if (productNodes!=null)
            {
                foreach(HtmlNode productNode in productNodes)
                {
                    HtmlNode imageNode = productNode.SelectSingleNode("a/div/div[@class='fs-product-card__product-image']");
                    HtmlNode nameNode = productNode.SelectSingleNode("a/div[@class='fs-product-card__description']/h3");
                    HtmlNode subnameNode = productNode.SelectSingleNode("a/div[@class='fs-product-card__description']/p");
                    HtmlNode dataNode = productNode.SelectSingleNode("div[@class='js-product-card-footer fs-product-card__footer-container']");

                    string styleImageUrl = imageNode.Attributes["data-src-s"].Value;//   //Regex.Match(imageNode.Attributes["style"].Value, @"\'([^']*)\'").Value;
                    string name = nameNode.InnerText.ToString().Trim();
                    string subname = subnameNode.InnerText.ToString().Trim();

                    string json = dataNode.Attributes["data-options"].Value;

                    XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), myReaderQuotas);
                    XElement rootNode = XElement.Load(jsonReader);

                    string productId = rootNode.Element("productId").Value;
                    double pricePerItem = Convert.ToDouble(rootNode.Element("ProductDetails").Element("PricePerItem").Value);

                    result.Add(new Product(productId, name, subname, styleImageUrl, pricePerItem));
                    if (result.Count == number)
                        break;
                }
            }
        }

        /// <summary>
        /// A method to call off to specific categories and return a list of grocery items
        /// </summary>
        public List<Product> GetProducts(string categoryUrl)
        {

            try
            {
                return GetProducts(categoryUrl, UInt32.MaxValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return new List<Product>();
        }

        /// <summary>
        /// A method to call off to specific categories and return a list of grocery items
        /// </summary>               
        public List<Product> GetProducts(string categoryUrl, uint number)
        {
            List<Product> result = new List<Product>();

            if (categoryUrl.StartsWith("/"))
                categoryUrl = categoryUrl.Substring(1);

            String url = "https://www.paknsave.co.nz/" + categoryUrl;

            bool stop = false;
            do
            {
                HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(url);
                request1.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request1.ServicePoint.Expect100Continue = false;
                request1.AllowAutoRedirect = false;
                request1.UserAgent = UserAgent;
                request1.Headers.Add("Upgrade-Insecure-Requests", "1");
                request1.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request1.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request1.CookieContainer = container;

                using (HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse())
                {
                    container.Add(response1.Cookies);
                    string content = GetContent(response1);
                    

                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(content);
                    //AddProducts(document, result, number);
                                       
                    HtmlNode requestVerificationTokenNode = document.DocumentNode.SelectSingleNode("//div[@class='component search-results vertical']");
                    if (requestVerificationTokenNode != null)
                    {

                    }
                    return new List<Product>();
                        
                        __requestVerificationToken = requestVerificationTokenNode.Attributes["value"].Value;

                    HtmlNode nextNode = document.DocumentNode.SelectSingleNode("//a[@aria-label='Next page']");
                    if (nextNode != null)
                    {
                        if (nextNode.Attributes["href"].Value.StartsWith("https://www.paknsave.co.nz"))
                            url = nextNode.Attributes["href"].Value;
                        else
                            url = "https://www.paknsave.co.nz" + nextNode.Attributes["href"].Value;
                    }
                    else
                        stop = true;
                }
            } while (!stop && result.Count < number);
            return result;
        }

        private static string UpperCaseUrlEncode(string s)
        {
            char[] temp = HttpUtility.UrlEncode(s).ToCharArray();
            for (int i = 0; i < temp.Length - 2; i++)
            {
                if (temp[i] == '%')
                {
                    temp[i + 1] = char.ToUpper(temp[i + 1]);
                    temp[i + 2] = char.ToUpper(temp[i + 2]);
                }
            }
            return new string(temp);
        }

        /// <summary>
        /// A method to imitate the search on the grocery website which will return a list of grocery items
        /// </summary>
        public List<Product> FindProducts(string findStr)
        {
            return FindProducts(findStr, UInt32.MaxValue);
        }

        /// <summary>
        /// A method to imitate the search on the grocery website which will return a list of grocery items
        /// </summary>               
        public List<Product> FindProducts(string findStr, uint number)
        {
            List<Product> result = new List<Product>();

            String url = "https://www.paknsave.co.nz/Search?q=" + UpperCaseUrlEncode(findStr);

            bool stop = false;
            do
            {
                HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(url);
                request1.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request1.ServicePoint.Expect100Continue = false;
                request1.AllowAutoRedirect = false;
                request1.UserAgent = UserAgent;
                request1.Headers.Add("Upgrade-Insecure-Requests", "1");
                request1.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request1.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request1.CookieContainer = container;

                using (HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse())
                {
                    container.Add(response1.Cookies);
                    string content = GetContent(response1);

                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(content);
                    AddProducts(document, result, number);

                    HtmlNode nextNode = document.DocumentNode.SelectSingleNode("//div[@class='component search-box horizontal']");
                    if (nextNode != null)
                    {

                        var sb = (from att in nextNode.Attributes
                                  where att.Name == "data-properties"
                                  select att.Value).FirstOrDefault();

                        if (!string.IsNullOrEmpty(sb))
                        {
                            if (SBox == null)
                            {
                                SBox = new SearchBoxInfoData();
                            }

                            SBox = JsonConvert.DeserializeObject<SearchBoxInfoData>(sb);

                            //https://www.paknsave.co.nz/BrandsApi/algoliasearch/results?s={F9E05712-3030-4B4E-BE27-C7174C53477F}&itemid={52A18E90-2448-44ED-89D1-E4973AC36EFC}&sig=&autoFireSearch=true&v=%7B4E225C7A-CE6B-4B24-A529-75929CE0E944%7D&p=20&e=0&q=milk
                            var searchUrl = "https://www.paknsave.co.nz/BrandsApi/algoliasearch/results?s=" + SBox.s + "&itemid=" + SBox.itemid + "&sig=&autoFireSearch=true&v=%7" + SBox.v.Replace("{", "").Replace("}","") + "%7D&p=20&e=0&q=" + findStr;


                            HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create(searchUrl);
                            request2.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                            request2.ServicePoint.Expect100Continue = false;
                            request2.AllowAutoRedirect = false;
                            request2.UserAgent = UserAgent;
                            request2.Headers.Add("Upgrade-Insecure-Requests", "1");
                            request2.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                            request2.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                            request2.CookieContainer = container;

                            //Host: www.paknsave.co.nz
                            //Connection: keep - alive
                            //sec - ch - ua: " Not A;Brand"; v = "99", "Chromium"; v = "98", "Microsoft Edge"; v = "98"
                            //Accept: application / json, text / javascript, */*; q=0.01
                            //DNT: 1
                            //X-Requested-With: XMLHttpRequest
                            //sec-ch-ua-mobile: ?0
                            //User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36 Edg/98.0.1108.62
                            //sec-ch-ua-platform: "Windows"
                            //Sec-Fetch-Site: same-origin
                            //Sec-Fetch-Mode: cors
                            //Sec-Fetch-Dest: empty
                            //Referer: https://www.paknsave.co.nz/search
                            //Accept-Encoding: gzip, deflate, br
                            //Accept-Language: en-US,en;q=0.9
                            //Cookie: SessionCookieIdV2=2f427721c9df47e4965633bda5acff0d; _gcl_au=1.1.1753424053.1645745079; _ga=GA1.1.1013997977.1645745079; brands_store_reset=true; eCom_StoreId_NotSameAs_Brands=False; brands_server_nearest_store={"StoreId":"{6961F035-6CA4-4F5A-9D7A-3228F9D3F5EB}","UserLat":"-35.693","UserLng":"174.3001","StoreLat":"-35.7260494719722","StoreLng":"174.324604973572","IsSuccess":true}; SC_ANALYTICS_GLOBAL_COOKIE=1d96fa63b45846dc9904d5b8bb13df04|True; Region=NI; AllowRestrictedItems=true; region_code=UNI; brands_browser_nearest_store={"StoreId":"{B8BDA472-6B4A-4ECA-9EDE-D0C2284A53B6}","UserLat":"-35.112007","UserLng":"173.26149","StoreLat":"-35.0992991646612","StoreLng":"173.258369030025","IsSuccess":true}; eCom_STORE_ID=3c5e3145-0767-4066-9349-6c0a1313acc5; brands_store_id={B8BDA472-6B4A-4ECA-9EDE-D0C2284A53B6}; eComm_Coordinate_Cookie={"latitude":-35.099299164661204,"longitude":173.25836903002471}; server_nearest_store_v2={"StoreId":"3c5e3145-0767-4066-9349-6c0a1313acc5","UserLat":"-35.0992991646612","UserLng":"173.258369030025","StoreLat":"-35.09945","StoreLng":"173.258322","IsSuccess":true}; STORE_ID_V2=3c5e3145-0767-4066-9349-6c0a1313acc5|False; sxa_site=Brand PAKnSAVE; ASP.NET_SessionId=u510kls1gtp433li4sqwnmvo; __RequestVerificationToken=1MOMT66_gFv4LO9VuHY6uPew81A4k5vcmqZsx6qZWUDHcl_kc9GmPHHF132swjvMAJ07C6qUuf6OKqKGWtGU5mKgRVs1; __cfruid=5e0eb4e51a86efeb2079b704013066d39eda6e7c-1646101174; __cf_bm=PalECn8MCY6IBvZy_pEQVBt_REoaT09lNqMhGz9Bhlw-1646101176-0-AbjgerYV6GWo3zB86CSXCQH3CsMYj4Q7B3CjNnEk05xjncyvZzy9KChRc1zoirq4OghuDqojxLSKdmoY5W2iB7qJj320OgsT0PMfCUxDjO2nnf+e6XHV3vgCyPwmaJXJeBm4c5HkqV9YSsScYYEppIlqQWlzs/QeDG2ixI1imxLL; _ga_8ZFCCVKEC2=GS1.1.1646101169.6.1.1646101190.39










                            using (HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse())
                            {




                                container.Add(response2.Cookies);
                                string content2 = GetContent(response2);

                                if (!string.IsNullOrEmpty(content2))
                                {
                                    var deserp = JsonConvert.DeserializeObject<SearchProduct>(content2);



                                }

                            }
                        }


                    }
                    else
                        stop = true;
                }
            } while (!stop && result.Count < number);
            return result;
        }

        SearchBoxInfoData SBox { get; set; }

        public StoreData ChooseStoreByName(List<Store> stores, string storeName)
        {

            try
            {
                foreach (var kvp in stores)
                {
                    if (kvp.name == storeName)
                    {
                        var url = "https://www.paknsave.co.nz/CommonApi/Store/ChangeStore?storeId=" + kvp.EcomStoreId;
                        HttpWebRequest request3 = (HttpWebRequest)WebRequest.Create(url);
                        request3.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                        request3.ServicePoint.Expect100Continue = false;
                        request3.AllowAutoRedirect = false;
                        request3.UserAgent = UserAgent;
                        request3.Headers.Add("Upgrade-Insecure-Requests", "1");
                        request3.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                        request3.Accept = "application/json, text/plain, */*";
                        request3.Method = "POST";
                        request3.CookieContainer = container;
                        request3.ContentLength = 0;

                        using (HttpWebResponse response3 = (HttpWebResponse)request3.GetResponse())
                        {
                            container.Add(response3.Cookies);

                            string content = GetContent(response3);

                            var sdata = JsonConvert.DeserializeObject<StoreData>(content);
                            if (SelectedStore == null)
                            {
                                SelectedStore = new StoreData();
                            }
                            SelectedStore = sdata;
                            return SelectedStore;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return new StoreData();
        }



        public bool Login(string email, string password)
        {           
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            byte[] jsonByte = Encoding.UTF8.GetBytes(serializer.Serialize(new Credentials(email, password)));

            try
            {
                HttpWebRequest request3 = (HttpWebRequest)WebRequest.Create("https://www.paknsave.co.nz/CommonApi/Account/Login");
                request3.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request3.ServicePoint.Expect100Continue = false;
                request3.AllowAutoRedirect = false;
                request3.UserAgent = UserAgent;
                request3.Headers.Add("Upgrade-Insecure-Requests", "1");
                request3.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request3.Headers.Add("X-NewRelic-ID", XNewRelicID);
                request3.Headers.Add("__RequestVerificationToken", __requestVerificationToken);
                request3.Accept = "application/json, text/plain, */*";
                request3.Method = "POST";
                request3.ContentType = "application/json;charset=UTF-8";
                request3.ContentLength = jsonByte.Length;
                request3.CookieContainer = container;

                using (Stream requestStream = request3.GetRequestStream())
                {
                    requestStream.Write(jsonByte, 0, jsonByte.Length);
                    requestStream.Close();
                }

                using (HttpWebResponse response3 = (HttpWebResponse)request3.GetResponse())
                {
                    container.Add(response3.Cookies);
                    string content = GetContent(response3);

                    XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(content), myReaderQuotas);
                    XElement rootNode = XElement.Load(jsonReader);

                    return rootNode.Element("success").Value.Trim().ToLowerInvariant() == "true";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return false;
        }

        private void PutProductToCart(Product product, int quantity, bool writeProduct = true)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            byte[] jsonByte = Encoding.UTF8.GetBytes(serializer.Serialize(new RequestProducts(product.Id, quantity)));

            try
            {
                HttpWebRequest request3 = (HttpWebRequest)WebRequest.Create("https://www.paknsave.co.nz/CommonApi/Cart/Index");
                request3.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request3.ServicePoint.Expect100Continue = false;
                request3.AllowAutoRedirect = false;
                request3.UserAgent = UserAgent;
                request3.Headers.Add("Upgrade-Insecure-Requests", "1");
                request3.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request3.Headers.Add("X-NewRelic-ID", XNewRelicID);
                request3.Headers.Add("__RequestVerificationToken", __requestVerificationToken);
                request3.Accept = "application/json, text/plain, */*";
                request3.Method = "POST";
                request3.ContentType = "application/json;charset=UTF-8";
                request3.ContentLength = jsonByte.Length;
                request3.CookieContainer = container;

                //if (writeProduct)
                //{
                    using (Stream requestStream = request3.GetRequestStream())
                    {
                        requestStream.Write(jsonByte, 0, jsonByte.Length);
                        requestStream.Close();
                    }
                //}

                //You must provide a request body if you set ContentLength>0 or SendChunked==true.  Do this by calling [Begin]GetRequestStream before [Begin]GetResponse.

                using (HttpWebResponse response3 = (HttpWebResponse)request3.GetResponse())
                {
                    container.Add(response3.Cookies);
                    string content = GetContent(response3);
                    var cart = JsonConvert.DeserializeObject<CartData>(content);

                    Debug.WriteLine("");
       
                }
            }
            catch (Exception ex)
            {
            }
        }

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }


        static async Task MainAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                List<Store> stores = new List<Store>();
                Program p = new Program();
                 p.FirstCall(ref stores);
                if (p.Login("2022datest@gmail.com", "Dev2022!"))
                {
                    var chosenStore = p.ChooseStoreByName(stores, "PAK'nSAVE Kaitaia");
                    
                    var navData = await p.GetCategories(chosenStore.storeDetails.id);


                    var url = (from n in navData.NavigationList
                               where n.Name.ToLower() == "groceries"
                               select n.Children).ToList();
                    if (url != null)
                    {
                        List<Product> products1 = p.GetProducts(url.FirstOrDefault().FirstOrDefault().URL);
                    }
                    //https://www.paknsave.co.nz/category/category/chilled-frozen-and-desserts/dairy--eggs/butter--spreads                
                    List<Product> products2 = p.FindProducts("milk");
                    //p.PutProductToCart(products1[1], 0, false);

                    Debug.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }
    }


}
