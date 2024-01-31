using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace ddns.net.cloud.godaddy
{
    public class GodaddyHttpClient
    {

        //https://developer.godaddy.com/doc/endpoint/domains#/
        private const string API_AddRecord = "/v1/domains/{domain}/records";
        private const string API_RecordList = "/v1/domains/{domain}/records/{type}/{name}";
        private const string API_EditRecord = "/v1/domains/{domain}/records/{type}/{name}";
        //private static string AK = "";
        //private static string SK = "";
        private readonly IHttpClientFactory httpClientFactory;
        public string AK { get; set; }
        public string SK { get; set; }
        public GodaddyHttpClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }



        /// <summary>
        /// 获取域名解析列表
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public async Task<List<GodaddyRecord>> GetRecords(string domain, string type, string rrname)
        {
            var api = API_RecordList.Replace("{domain}", domain).Replace("{type}", type).Replace("{name}", rrname);
            var response = await HttpRequest(null, api, HttpMethod.Get);

            var contentstr = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<List<GodaddyRecord>>(contentstr);
            }
            else
            {
                Serilog.Log.Debug($" godaddy get {domain} dns records error {contentstr}");
                return null;
            }
        }

        /// <summary>
        /// 新增域名解析
        /// </summary>
        /// <param name="recordRequest"></param>
        /// <returns></returns>
        public async Task<bool> AddRecord(GodaddyAddRecordRequest recordRequest)
        {
            var api = API_AddRecord.Replace("{domain}", recordRequest.domain);

            var response = await HttpRequest(recordRequest.records, api, HttpMethod.Patch);
            var record = recordRequest.records.FirstOrDefault();
            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                //var contentstr = await response.Content.ReadAsStringAsync();
                //return string.IsNullOrEmpty(contentstr);//返回为空 则说明添加成功
                Serilog.Log.Debug($" godaddy add dns record {recordRequest.domain},rr={record.name},type={record.type}，value={record.data} success");
                return true;
            }
            else
            {
                var contentstr = await response.Content.ReadAsStringAsync();
                Serilog.Log.Debug($" godaddy add dns record {recordRequest.domain},rr={record.name},type={record.type}，value={record.data}  error {contentstr}");
                return false;
            }
        }


        public async Task<bool> UpdateRecord(GodaddyEditRecordRequest recordRequest)
        {
            var api = API_EditRecord.Replace("{domain}", recordRequest.domain).Replace("{type}", recordRequest.type).Replace("{name}", recordRequest.name);

            var response = await HttpRequest(recordRequest.records, API_EditRecord, HttpMethod.Put);

            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                //var contentstr = await response.Content.ReadAsStringAsync();
                //return string.IsNullOrEmpty(contentstr);//返回为空 则说明添加成功
                Serilog.Log.Debug($"godaddy edit dns record {recordRequest.domain},rr={recordRequest.name},type={recordRequest.type}，value={recordRequest.records.FirstOrDefault()?.data} success");
                return true;
            }
            else
            {
                var contentstr = await response.Content.ReadAsStringAsync();
                Serilog.Log.Debug($" godaddy edit dns record {recordRequest.domain},rr={recordRequest.name},type={recordRequest.type}，value={recordRequest.records.FirstOrDefault()?.data} error {contentstr}");
                return false;
            }

        }

        private async Task<HttpResponseMessage> HttpRequest(object? request, string Api, HttpMethod method)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("Godaddy");
                httpClient.DefaultRequestHeaders.Add("Authorization", $"sso-key {AK}:{SK}");
                var requestMessage = new HttpRequestMessage(method, Api);
                if (request != null)
                {
                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                }
                var response = await httpClient.SendAsync(requestMessage);
                return response;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($" godaddy http request {Api}, fail {ex.StackTrace}");
                return null;
            }
        }

    }
}
