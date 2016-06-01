using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        private static async Task<T> HandleResultStatusCode<T>(HttpResponseMessage messageResponse, Func<string, Task<T>> handleFunc)
        {
            if (messageResponse.IsSuccessStatusCode)
            {
                var response = await messageResponse.Content.ReadAsStringAsync();
                return await handleFunc(response);
            }
            else
            {
                if (messageResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new APIClientUnathorizedException();
                }
                else
                {
                    throw new APIClientException($"Your request is {messageResponse.StatusCode.ToString()}");
                }
            }
        }

        public async static Task<string> GetBearerToken(string server, string username, string password, string tenant)
        {
            using (var client = APIClientHelper.GetInsecureHttpClient())
            {
                try
                {
                    string uri = $"https://{server}/identity/api/tokens";
                    HttpResponseMessage messageResponse = await client.PostAsync(
                        uri,
                        new StringContent(
                            $"{{\"username\":\"{username}\",\"password\":\"{password}\",\"tenant\":\"{tenant}\"}}",
                            Encoding.UTF8,
                            "application/json"));

                    return await HandleResultStatusCode<string>(messageResponse, (response) =>
                    {
                        var xml = XDocument.Parse(response);
                        var id = (from el in xml.Root.Descendants("id") select el).First();
                        return Task.FromResult<string>(id.Value);
                    });
                }
                catch (Exception ex)
                {
                    throw new APIClientException("Something went terribly wrong!", ex);
                }
            }
        }

        public async static Task<IEnumerable<string>> GetCatalogItems(string server, string bearerToken)
        {
            using (var client = APIClientHelper.GetInsecureHttpClientWithToken(bearerToken))
            {
                try
                {
                    HttpResponseMessage messageResponse = await client.GetAsync(
                        $"https://{server}/catalog-service/api/consumer/entitledCatalogItemViews");

                    return await HandleResultStatusCode<IEnumerable<string>>(messageResponse, (response) =>
                    {
                        var xml = XDocument.Parse(response);
                        var catalogItemNames = from el in xml.Root.Descendants("name") select el.Value;
                        return Task.FromResult<IEnumerable<string>>(catalogItemNames);
                    });
                }
                catch (Exception ex)
                {
                    throw new APIClientException("Something went terribly wrong!", ex);
                }
            }
        }

        public async static Task<string> RequestCatalogItem(string server, string bearerToken, string catalogItem)
        {
            using (var client = APIClientHelper.GetInsecureHttpClientWithToken(bearerToken))
            {
                try
                {
                    HttpResponseMessage messageResponse = await client.GetAsync(
                        $"https://{server}/catalog-service/api/consumer/entitledCatalogItemViews");

                    return await HandleResultStatusCode<string>(messageResponse, async (response) =>
                    {
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

                                return await HandleResultStatusCode<string>(msgTemplate, async (templateResponse) =>
                                {
                                    HttpResponseMessage msgItemRequest = await jsonClient.PostAsync(
                                        $"https://{server}/catalog-service/api/consumer/entitledCatalogItems/{itemId.Value}/requests",
                                        new StringContent(
                                            templateResponse,
                                            Encoding.UTF8,
                                            "application/json"));

                                    return await HandleResultStatusCode<string>(msgItemRequest, (itemResponse) =>
                                    {
                                        var item = JObject.Parse(itemResponse);
                                        return Task.FromResult<string>(item.Value<string>("id"));
                                    });
                                });
                            }
                        }

                        throw new APIClientException($"The catalog item {catalogItem} is no longer available for request!");
                    });
                }
                catch (Exception ex)
                {
                    throw new APIClientException("Something went terribly wrong!", ex);
                }
            }
        }

        public async static Task<string> GetRequestState(string server, string bearerToken, string requestId)
        {
            using (var client = APIClientHelper.GetInsecureHttpClientWithToken(bearerToken))
            {
                try
                {
                    HttpResponseMessage messageResponse = await client.GetAsync(
                        $"https://{server}/catalog-service/api/consumer/requests/{requestId}");

                    return await HandleResultStatusCode<string>(messageResponse, (response) =>
                    {
                        var xml = XDocument.Parse(response);
                        var state = xml.Root.Attribute("state").Value;
                        return Task.FromResult<string>(state);
                    });
                }
                catch (Exception ex)
                {
                    throw new APIClientException("Something went terribly wrong!", ex);
                }
            }
        }
    }
}