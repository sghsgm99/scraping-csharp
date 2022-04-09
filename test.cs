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
public string email { get; set; }
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

string styleImageUrl = imageNode.Attributes["data-src-s"].Value;// //Regex.Match(imageNode.Attributes["style"].Value, @"\'([^']*)\'").Value;
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

String url = "https://www.paknsave.co.nz/shop/Search?q=" + UpperCaseUrlEncode(findStr);

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