using Newtonsoft.Json;

namespace ddns.net.cloud.cz88
{
    public class IpHttpClient
    {

        private readonly IHttpClientFactory _clientFactory;
        public IpHttpClient(IHttpClientFactory httpClientFactory)
        {
            _clientFactory = httpClientFactory;
        }
        public async Task<CZ88IpData> HttpGetIpInfo()
        {
            try
            {
                var httpClient = _clientFactory.CreateClient();

                httpClient.BaseAddress = new Uri("https://www.cz88.net");//https://ip.cn
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
                var res = await httpClient.GetAsync("/api/cz88/ip/base?ip=");//  /api/index?ip=&type=0  ip.cn
                if (res != null && res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadAsStringAsync();
                    //Serilog.Log.Debug(data);
                    var cz88 = JsonConvert.DeserializeObject<CZ88IpInfo>(data);
                    return cz88?.data;
                }
                else
                {
                    Serilog.Log.Error($"GetIpInfo error, StatusCode={(res == null ? "no response" : res?.StatusCode)}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"GetIpInfo error: {ex.Message}");
                return null;
            }
        }

    }

    public class GetIpApiResult
    {
        /// <summary>
        /// 
        /// </summary>
        public int rs { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 中国  湖南省 长沙市 电信
        /// </summary>
        public string address { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ip { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int isDomain { get; set; }
    }



    public class CZ88IpData
    {
        /// <summary>
        /// 
        /// </summary>
        public string ip { get; set; }
        /// <summary>
        /// 中国
        /// </summary>
        public string country { get; set; }
        /// <summary>
        /// 湖北
        /// </summary>
        public string province { get; set; }
        /// <summary>
        /// 武汉
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// 中国电信
        /// </summary>
        public string isp { get; set; }
    }

    public class CZ88IpInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string success { get; set; }
        /// <summary>
        /// 操作成功
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public CZ88IpData data { get; set; }
    }

}
