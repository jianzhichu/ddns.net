using ddns.net.extension;
using ddns.net.model;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ddns.net.cloud.tencent
{
    public class TencentHttpClient
    {
        private readonly IHttpClientFactory httpClientFactory;

        public TencentHttpClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// action CreateRecord https://cloud.tencent.com/document/api/1427/56180
        /// action ModifyRecord https://cloud.tencent.com/document/api/1427/56157
        /// action DescribeRecordList https://cloud.tencent.com/document/api/1427/56166
        /// action DescribeDomain https://cloud.tencent.com/document/api/1427/56173
        /// </summary>
        /// <typeparam name="Request"></typeparam>
        /// <typeparam name="Response"></typeparam>
        /// <param name="config"></param>
        /// <param name="request"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<Response> Request<Request, Response>(DomainConfigInfo config, Request request,string action) where Request : class where Response : class
        {
            string jsonData = string.Empty;
            try
            {
                var payload = JsonConvert.SerializeObject(request, Formatting.None,
                  new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                using var client = httpClientFactory.CreateClient();
                //client.BaseAddress = new Uri("https://dnspod.tencentcloudapi.com");
                var headers = BuildHeaders(config.AK, config.SK, action, payload);
                var content = new StringContent(payload);
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    if (kvp.Key.Equals("Content-Type"))
                    {
                        content.Headers.Remove("Content-Type");
                        content.Headers.Add("Content-Type", kvp.Value);
                        //msg.Content = content;
                    }
                    else if (kvp.Key.Equals("Host"))
                    {
                        client.DefaultRequestHeaders.Host = kvp.Value;
                    }
                    else if (kvp.Key.Equals("Authorization"))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TC3-HMAC-SHA256",
                            kvp.Value.Substring("TC3-HMAC-SHA256".Length));
                    }
                    else
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    }
                }

                var responseData = await client.PostAsync("https://dnspod.tencentcloudapi.com", content);

                jsonData = await responseData.Content.ReadAsStringAsync();

                var response = JsonConvert.DeserializeObject<Response>(jsonData);

                return response;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"aliyun  Request {action} error,{ex},response={jsonData}");
                return null;
            }
        }

       static Dictionary<string, string> BuildHeaders(string secretid,string secretkey,   string action, string requestPayload)
        {
            var endpoint = "dnspod.tencentcloudapi.com";
            var version = "2021-03-23";
            var service = endpoint.Split(".")[0];
            long timestamp = Util.ToTimestamp() / 1000;
            string requestTimestamp = timestamp.ToString();
            var minDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            string cerdate = minDate.AddSeconds(timestamp).ToString("yyyy-MM-dd");
            // ************* 步骤 1：拼接规范请求串 *************
            string algorithm = "TC3-HMAC-SHA256";
            string httpRequestMethod = "POST";
            string canonicalUri = "/";
            string canonicalQueryString = "";
            string contentType = "application/json";
            string canonicalHeaders = "content-type:" + contentType + "; charset=utf-8\n"
                + "host:" + endpoint + "\n"
                + "x-tc-action:" + action.ToLower() + "\n";
            string signedHeaders = "content-type;host;x-tc-action";
            string hashedRequestPayload = Util.SHA256Hex(requestPayload);
            string canonicalRequest = httpRequestMethod + "\n"
                + canonicalUri + "\n"
                + canonicalQueryString + "\n"
                + canonicalHeaders + "\n"
                + signedHeaders + "\n"
                + hashedRequestPayload;
            //Console.WriteLine(canonicalRequest);

            // ************* 步骤 2：拼接待签名字符串 *************
            string credentialScope = cerdate + "/" + service + "/" + "tc3_request";
            string hashedCanonicalRequest = Util.SHA256Hex(canonicalRequest);
            string stringToSign = algorithm + "\n"
                + requestTimestamp + "\n"
                + credentialScope + "\n"
                + hashedCanonicalRequest;
            Console.WriteLine(stringToSign);

            // ************* 步骤 3：计算签名 *************
            byte[] tc3SecretKey = Encoding.UTF8.GetBytes("TC3" + secretkey);
            byte[] secretDate = Util.HmacSHA256(tc3SecretKey, Encoding.UTF8.GetBytes(cerdate));
            byte[] secretService = Util.HmacSHA256(secretDate, Encoding.UTF8.GetBytes(service));
            byte[] secretSigning = Util.HmacSHA256(secretService, Encoding.UTF8.GetBytes("tc3_request"));
            byte[] signatureBytes = Util.HmacSHA256(secretSigning, Encoding.UTF8.GetBytes(stringToSign));
            string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();
            Console.WriteLine(signature);

            // ************* 步骤 4：拼接 Authorization *************
            string authorization = algorithm + " "
                + "Credential=" + secretid + "/" + credentialScope + ", "
                + "SignedHeaders=" + signedHeaders + ", "
                + "Signature=" + signature;
            Console.WriteLine(authorization);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", authorization },
                { "Host", endpoint },
                { "Content-Type", contentType + "; charset=utf-8" },
                { "X-TC-Timestamp", requestTimestamp.ToString() },
                { "X-TC-Version", version },
                { "X-TC-Action", action }
            };
            //headers.Add("X-TC-Region", region);
            return headers;
        }
    }



    #region 请求实体类

    public class CreateRecordRequest
    {

        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// 记录类型，通过 API 记录类型获得，大写英文，比如：A 。
        /// </summary>
        public string RecordType { get; set; }

        /// <summary>
        /// 记录线路，通过 API 记录线路获得，中文，比如：默认。
        /// </summary>
        public string RecordLine { get; set; }

        /// <summary>
        /// 记录值，如 IP : 200.200.200.200， CNAME : cname.dnspod.com.， MX : mail.dnspod.com.。
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 域名 ID 。参数 DomainId 优先级比参数 Domain 高，如果传递参数 DomainId 将忽略参数 Domain 。
        /// </summary>
        public ulong? DomainId { get; set; }

        /// <summary>
        /// 主机记录，如 www，如果不传，默认为 @。
        /// </summary>
        public string SubDomain { get; set; }
    }



    public class DescribeRecordListRequest 
    {

        /// <summary>
        /// 要获取的解析记录所属的域名
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// 要获取的解析记录所属的域名Id，如果传了DomainId，系统将会忽略Domain参数。 可以通过接口DescribeDomainList查到所有的Domain以及DomainId
        /// </summary>
        public ulong? DomainId { get; set; }

        /// <summary>
        /// 解析记录的主机头，如果传了此参数，则只会返回此主机头对应的解析记录
        /// </summary>
        public string Subdomain { get; set; }

        /// <summary>
        /// 获取某种类型的解析记录，如 A，CNAME，NS，AAAA，显性URL，隐性URL，CAA，SPF等
        /// </summary>
        public string RecordType { get; set; }

    }

    public class ModifyRecordRequest
    {

        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// 记录类型，通过 API 记录类型获得，大写英文，比如：A 。
        /// </summary>
        public string RecordType { get; set; }

        /// <summary>
        /// 记录线路，通过 API 记录线路获得，中文，比如：默认。
        /// </summary>
        public string RecordLine { get; set; }

        /// <summary>
        /// 记录值，如 IP : 200.200.200.200， CNAME : cname.dnspod.com.， MX : mail.dnspod.com.。
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 记录 ID 。可以通过接口DescribeRecordList查到所有的解析记录列表以及对应的RecordId
        /// </summary>
        public ulong? RecordId { get; set; }

        /// <summary>
        /// 域名 ID 。参数 DomainId 优先级比参数 Domain 高，如果传递参数 DomainId 将忽略参数 Domain 。可以通过接口DescribeDomainList查到所有的Domain以及DomainId
        /// </summary>
        public ulong? DomainId { get; set; }

        /// <summary>
        /// 主机记录，如 www，如果不传，默认为 @。
        /// </summary>
        public string SubDomain { get; set; }
    }



    public class DescribeDomainRequest
    {

        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// 域名 ID 。参数 DomainId 优先级比参数 Domain 高，如果传递参数 DomainId 将忽略参数 Domain 。可以通过接口DescribeDomainList查到所有的Domain以及DomainId
        /// </summary>
        public ulong? DomainId { get; set; }

    }
    #endregion

    #region 响应实体类

    public class CreateOrModifyRecordResponse 
    {

        /// <summary>
        /// 记录ID
        /// </summary>
        public ulong? RecordId { get; set; }

        /// <summary>
        /// 唯一请求 ID，每次请求都会返回。定位问题时需要提供该次请求的 RequestId。
        /// </summary>
        public string RequestId { get; set; }
    }


    public class DescribeRecordListResponse
    {

        /// <summary>
        /// 获取的记录列表
        /// </summary>
        public RecordListItem[] RecordList { get; set; }

        /// <summary>
        /// 唯一请求 ID，每次请求都会返回。定位问题时需要提供该次请求的 RequestId。
        /// </summary>
        public string RequestId { get; set; }


    }

    public class RecordCountInfo
    {

        /// <summary>
        /// 子域名数量
        /// </summary>
        public ulong? SubdomainCount { get; set; }

        /// <summary>
        /// 列表返回的记录数
        /// </summary>
        public ulong? ListCount { get; set; }

        /// <summary>
        /// 总的记录数
        /// </summary>
        public ulong? TotalCount { get; set; }

    }


    public class RecordListItem
    {

        /// <summary>
        /// 记录Id
        /// </summary>
        public ulong? RecordId { get; set; }

        /// <summary>
        /// 记录值
        /// </summary>
        public string Value { get; set; }
    }

    public class DescribeDomainResponse 
    {

        /// <summary>
        /// 域名信息
        /// </summary>
        public DomainInfo DomainInfo { get; set; }

        /// <summary>
        /// 唯一请求 ID，每次请求都会返回。定位问题时需要提供该次请求的 RequestId。
        /// </summary>
        public string RequestId { get; set; }


    }


    public class DomainInfo {  

        /// <summary>
        /// 域名ID
        /// </summary>
        public ulong? DomainId { get; set; }
    }
    #endregion
}
