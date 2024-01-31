using ddns.net.model;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace ddns.net.cloud.huawei
{
    public class HuaweiHttpClient
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly HuaweiSigner huaweiSigner;

        public HuaweiHttpClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
            this.huaweiSigner = huaweiSigner;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Request"></typeparam>
        /// <typeparam name="Response"></typeparam>
        /// <param name="config"></param>
        /// <param name="request"></param>
        /// <param name="zoneId"></param>
        /// <param name="recordId"></param>
        /// <param name="query"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<Response> Request<Request, Response>(DomainConfigInfo config, Request request, string zoneId = "", string recordId = "", string query = "", string method = "Post") where Request : class where Response : class
        {
            string jsonData = string.Empty;
            var url = string.IsNullOrEmpty(zoneId) ? "v2/zones" :
                $"v2/zones/{zoneId}/recordsets{(string.IsNullOrEmpty(recordId) ? "" : $"/{recordId}")}";
            if (!string.IsNullOrEmpty(query))
                url += query;

            try
            {

                var payload = method == "Get"||request is null ? "" : JsonConvert.SerializeObject(request, Formatting.None,
                  new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                using var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://dnspod.tencentcloudapi.com");

                new HuaweiSigner().Sign(client, payload, url, config.AK, config.SK);

                if (method == "Post")
                {
                    var res = await client.GetAsync(url);
                    var content = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Response>(content);
                }
                else
                {
                    HttpContent httpContent = new StringContent(payload);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    var res = await client.PostAsync(url, httpContent);
                    var content = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Response>(content);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"huawei  Request {url} error,{ex},response={jsonData}");
                return null;
            }
        }


        /// <summary>
        /// 查询单个Zone下Record Set列表
        /// </summary>
        /// https://support.huaweicloud.com/api-dns/dns_api_64004.html
        /// <param name="request"></param>
        /// <param name="config"></param>
        /// <param name="zoneId"></param>
        /// <returns></returns>
        public async Task<ListRecordSetsByZoneResponse> ListRecordSetsByZoneAsync(ListRecordSetsByZoneRequest request, DomainConfigInfo config, string zoneId)
        {
            var query = $"name={request.name}&type={request.type}";
            return await Request<ListRecordSetsByZoneRequest, ListRecordSetsByZoneResponse>(config, null, zoneId, null, query, "Get");
        }

        /// <summary>
        /// 创建单个Record Set
        /// </summary>
        /// https://support.huaweicloud.com/api-dns/dns_api_64001.html
        /// <param name="request"></param>
        /// <param name="config"></param>
        /// <param name="zoneId"></param>
        /// <returns></returns>
        public async Task<CreateRecordSetResponse> CreateRecordSetAsync(CreateRecordSetRequestBody request,DomainConfigInfo config,string zoneId)
        {
            return await Request<CreateRecordSetRequestBody, CreateRecordSetResponse>(config, request, zoneId);
        }

        /// <summary>
        /// 查询单个Zone下Record Set列表
        /// </summary>
        /// https://support.huaweicloud.com/api-dns/dns_api_64004.html
        /// <param name="request"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<ListPublicZonesResponse> ListPublicZonesAsync(ListPublicZonesRequest request, DomainConfigInfo config)
        {
            var query = $"?name={request.name}";
            return await Request<ListPublicZonesRequest, ListPublicZonesResponse>(config, null, "","",query,"Get");
        }

        /// <summary>
        /// 修改单个Record Set
        /// </summary>
        /// https://support.huaweicloud.com/api-dns/UpdateRecordSet.html
        /// <param name="request"></param>
        /// <param name="config"></param>
        /// <param name="zoneId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public async Task<UpdateRecordSetResponse> UpdateRecordSetAsync(UpdateRecordSetRequestBody request, DomainConfigInfo config, string zoneId, string recordId)
        {
            return await Request<UpdateRecordSetRequestBody, UpdateRecordSetResponse>(config, request, zoneId, recordId);
        }


    }


    #region 实体类


    #region 获取zoneId
    public class ListPublicZonesRequest
    {
        public string name { get; set; }
    }

    public class ListPublicZonesResponse
    {
        public List<PublicZoneResp> Zones { get; set; }
    }

    public class PublicZoneResp
    {
        public string Id { get; set; }
    }
    #endregion

    #region 创建记录


    public class CreateRecordSetRequestBody
    {
        public string name { get; set; }
        public string type { get; set; }

        public List<string> records { get; set; }
    }

    public class CreateRecordSetResponse
    {
        public string id { get; set; }
        public string zone_id { get; set; }
    }

    #endregion

    #region 修改记录

    public class UpdateRecordSetRequestBody
    {
        public string name { get; set; }
        public string type { get; set; }

        public List<string> records { get; set; }
    }

    public class UpdateRecordSetResponse
    {
        public string id { get; set; }
    }
    #endregion

    #region 解析列表


    public class ListRecordSetsByZoneRequest
    {
        public string type { get; set; }
        public string name { get; set; }

    }

    public class ListRecordSetsByZoneResponse
    {
        public List<ListRecordSets> recordsets { get; set; }
    }

    public class ListRecordSets
    {
        /// <summary>
        /// 域名解析后的值。
        /// </summary>
        public List<string> records { get; set; }
    }
    #endregion
    #endregion
}
