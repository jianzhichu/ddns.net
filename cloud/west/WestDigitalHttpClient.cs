using Newtonsoft.Json;
using System.Reflection;

namespace ddns.net.cloud.west
{
    public class WestDigitalHttpClient
    {

        //https://console-docs.apipost.cn/preview/bf57a993975b67e0/7b363d9b8808faa2?target_id=74e45959-51c8-42ff-a359-8a7d2548dc89
        public string API = "https://api.west.cn/API/v2/domain/dns/";//api地址
        //private static string AK = "";//用户名  您在我司的用户名
        //private static string SK = "";//key 	api密码 32位MD5
        //private static string API_KEY = "";//域名key 32位apikey，为域名管理密码的md5值，可在域名控制面板中直接复制
        //帐号认证和域名认证二选一
        private static string API_LIST = "dnsrec.list";//列表
        private static string API_MODIFY = "dnsrec.modify";//修改
        private static string API_ADD = "dnsrec.add"; //新增

        private static HttpClient httpClient = new HttpClient();



        /// <summary>
        /// 获取域名解析列表
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public async Task<List<WestDigitalItem>> GetRecords(WestDigitalRequest request)
        {
            request.act = API_LIST;
            var body = await HttpRequest<WestItemResponseBody>(request);
            if (body != null)
                return body.items;
            return null;
        }

        /// <summary>
        /// 新增域名解析
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<int> AddRecord(WestDigitalRequest request)
        {

            request.act = API_ADD;
            var response = await HttpRequest<WestDigitalAddRecord>(request);
            if (response != null)
            {
                return response.record_id;
            }
            return 0;
        }


        public async Task<bool> UpdateRecord(WestDigitalRequest request)
        {
            request.act = API_MODIFY;
            var response = await HttpRequest<WestDigitalEditRecord>(request);
            return response != null && response.success != null && response.success.Count > 0
                && response.success.FirstOrDefault().ToString() == request.record_id;
        }

        private async Task<T> HttpRequest<T>(WestDigitalRequest request) where T : class
        {
            try
            {
                var dic = ObjectToDictionary(request);
                var formContent = new FormUrlEncodedContent(dic);
                var response = await httpClient.PostAsync(API, formContent);

                var contentstr = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var data = JsonConvert.DeserializeObject<WestDigitalResponse<T>>(contentstr);
                    if (data != null && data.code == 200)
                    {
                        return data.body;
                    }
                    Serilog.Log.Debug($" west get {request.act} dns records is null");
                    return null;
                }
                else
                {
                    Serilog.Log.Debug($" west get {request.act} dns records error {contentstr}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($" west http request {request.act}, fail {ex.StackTrace}");
                return null;
            }
        }

        static Dictionary<string, string> ObjectToDictionary(object obj)
        {
            var dict = new Dictionary<string, string>();

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.GetValue(obj) != null)
                    dict.Add(property.Name, property.GetValue(obj).ToString());
            }

            return dict;
        }

    }
}

