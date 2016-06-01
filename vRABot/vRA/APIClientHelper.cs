using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace vRABot.vRA
{
    public static class APIClientHelper
    {
        private static HttpClient GetInsecureHttpClient(string acceptType = "application/xml")
        {
            var handler = new WebRequestHandler();
            handler.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => { return true; };
            var client = new HttpClient(handler, true);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));

            return client;
        }

        private static HttpClient GetInsecureHttpClientWithToken(string token, string acceptType = "application/xml")
        {
            var client = GetInsecureHttpClient(acceptType);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        public async static Task<string> GetBearerToken(string server, string username, string password, string tenant)
        {
            using (var client = APIClientHelper.GetInsecureHttpClient())
            {
                try
                {
                    string uri = $"https://{server}/identity/api/tokens";
                    HttpResponseMessage msg = await client.PostAsync(
                        uri,
                        new StringContent(
                            $"{{\"username\":\"{username}\",\"password\":\"{password}\",\"tenant\":\"{tenant}\"}}",
                            Encoding.UTF8,
                            "application/json"));

                    if (msg.IsSuccessStatusCode)
                    {
                        var response = await msg.Content.ReadAsStringAsync();
                        var xml = XDocument.Parse(response);
                        var id = (from el in xml.Root.Descendants("id") select el).First();
                        return id.Value;
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public async static Task<IEnumerable<string>> GetCatalogItems(string server, string bearerToken)
        {
            using (var client = APIClientHelper.GetInsecureHttpClientWithToken(bearerToken))
            {
                try
                {
                    string uri = $"https://{server}/catalog-service/api/consumer/entitledCatalogItemViews";
                    HttpResponseMessage msg = await client.GetAsync(uri);

                    if (msg.IsSuccessStatusCode)
                    {
                        var response = await msg.Content.ReadAsStringAsync();
                        var xml = XDocument.Parse(response);
                        var catalogItemNames = from el in xml.Root.Descendants("name") select el.Value;
                        return catalogItemNames;
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public async static Task<bool> RequestCatalogItem(string server, string bearerToken, string catalogItem)
        {
            using (var client = APIClientHelper.GetInsecureHttpClientWithToken(bearerToken))
            {
                try
                {
                    HttpResponseMessage msg = await client.GetAsync(
                        $"https://{server}/catalog-service/api/consumer/entitledCatalogItemViews");

                    if (msg.IsSuccessStatusCode)
                    {
                        var response = await msg.Content.ReadAsStringAsync();
                        var xml = XDocument.Parse(response);
                        var itemId = (from el in xml.Root.Descendants("consumerEntitledCatalogItemView")
                                      where el.Element("name").Value.Equals(catalogItem, StringComparison.InvariantCultureIgnoreCase)
                                      select el.Element("catalogItemId")).FirstOrDefault();

                        if (itemId != null)
                        {
                            using (var jsonClient = APIClientHelper.GetInsecureHttpClientWithToken(bearerToken, "application/json"))
                            {
                                HttpResponseMessage msgTemplate = await jsonClient.GetAsync(
                                    $"https://{server}/catalog-service/api/consumer/entitledCatalogItems/{itemId.Value}/requests/template");

                                if (msgTemplate.IsSuccessStatusCode)
                                {
                                    var requestTemplate = await msgTemplate.Content.ReadAsStringAsync();
                                    HttpResponseMessage msgItemRequest = await client.PostAsync(
                                        $"https://{server}/catalog-service/api/consumer/entitledCatalogItems/{itemId.Value}/requests",
                                        new StringContent(
                                            requestTemplate,
                                            Encoding.UTF8,
                                            "application/json"));

                                    if (msgItemRequest.IsSuccessStatusCode)
                                    {
                                        var itemRequest = await msgItemRequest.Content.ReadAsStringAsync();
                                        return true;
                                    }
                                }
                            }

                        }
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}