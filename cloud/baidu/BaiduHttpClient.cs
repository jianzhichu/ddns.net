using ddns.net.model;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net.Http;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ddns.net.cloud.baidu
{
    public class BaiduHttpClient
    {

        //https://cloud.baidu.com/doc/BCD/s/4jwvymhs7#删除域名解析记录
        //public const string BaidApi = "https://bcd.baidubce.com";
        private const string API_RecordList = "list";
        private const string API_AddRecord = "add";
        private const string API_EditRecord = "edit";
        private readonly IHttpClientFactory httpClientFactory;

        public BaiduHttpClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 获取域名解析列表
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public async Task<BaiduRecordResult> GetRecords(string domain, DomainConfigInfo config)
        {
            var requestBody = new { domain, pageNo = 1, pagesize = 100 };//一般用户也不会有多少，写死100
            var response = await HttpRequest(requestBody, API_RecordList, config.AK, config.SK);

            var contentstr = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<BaiduRecordResult>(contentstr);
            }
            else
            {
                Serilog.Log.Error($" baidu get {domain} dns records error {contentstr}");
                return null;
            }
        }

        /// <summary>
        /// 新增域名解析
        /// </summary>
        /// <param name="recordRequest"></param>
        /// <returns></returns>
        public async Task<bool> AddRecord(BaiduAddRecordRequest recordRequest, DomainConfigInfo config)
        {
            var response = await HttpRequest(recordRequest, API_AddRecord, config.AK, config.SK);

            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                var contentstr = await response.Content.ReadAsStringAsync();
                Serilog.Log.Error($" baidu add dns record {recordRequest.zoneName},rr={recordRequest.domain},type={recordRequest.rdType}，value={recordRequest.rdata}  error {contentstr}");
                return false;
            }
        }


        public async Task<bool> UpdateRecord(BaiduEditRecordRequest recordRequest,DomainConfigInfo config)
        {

            var response = await HttpRequest(recordRequest, API_EditRecord, config.AK, config.SK);

            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                //var contentstr = await response.Content.ReadAsStringAsync();
                //return string.IsNullOrEmpty(contentstr);//返回为空 则说明添加成功
                Serilog.Log.Debug($"baidu edit dns record {recordRequest.zoneName},rr={recordRequest.domain},type={recordRequest.rdType}，value={recordRequest.rdata} success");
                return true;
            }
            else
            {
                var contentstr = await response.Content.ReadAsStringAsync();
                Serilog.Log.Error($" baidu edit dns record {recordRequest.zoneName},rr={recordRequest.domain},type={recordRequest.rdType}，value={recordRequest.rdata} error {contentstr}");
                return false;
            }

        }

        private async Task<HttpResponseMessage> HttpRequest(object request, string Api,string AK,string SK)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri("https://bcd.baidubce.com");
                var content = new StringContent(JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var headers = BaiduSigner.GetAuthHeaders(AK,SK,HttpMethod.Post, $"/v1/domain/resolve/{Api}");

                foreach (var item in headers)
                {
                    if (httpClient.DefaultRequestHeaders.Contains(item.Key))
                        httpClient.DefaultRequestHeaders.Remove(item.Key);
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(item.Key, item.Value);
                }
                var response = await httpClient.PostAsync($"{Api}", content);
                return response;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($" baidu http request {Api}, fail {ex.StackTrace}");
                return null;
            }
        }

    }
}
