using ddns.net.extension;
using ddns.net.model;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ddns.net.cloud.aliyun
{
    public class AliyunHttpClient
    {
        private readonly IHttpClientFactory  httpClientFactory;
        //private const string dnsEndpoint = "https://alidns.aliyuncs.com/";

        public AliyunHttpClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }


        /// <summary>
        /// 请求阿里云域名解析接口
        /// </summary>
        /// <param name="config"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <summary>
        /// action AddDomainRecord  https://help.aliyun.com/document_detail/2355674.html
        /// action UpdateDomainRecord https://help.aliyun.com/document_detail/2355677.html
        /// action DescribeDomainRecords  https://help.aliyun.com/document_detail/2357159.html
        public async Task<Response> Request<Request, Response>(DomainConfigInfo config, Request request) where Request : AliyunBaseRequest where Response : class, new()
        {
            var content = string.Empty;
            try
            {
                var dic = request.SimpleObjConvertToDic();

                var query = SignAndRequestParams(config.AK, config.SK, dic);

                using var client = httpClientFactory.CreateClient();
                var requestUri = $"https://alidns.aliyuncs.com/?{query}";
                var response = await client.GetAsync(requestUri);
                content = await response.Content.ReadAsStringAsync();

                var res = JsonConvert.DeserializeObject<Response>(content);
                return res;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"aliyun  Request {request.Action} error,{ex},response={content}");
            }
            return null;
        }


        private static StringBuilder SignAndRequestParams(string accessKeyID, string accessSecret, Dictionary<string, string> parameters)
        {
            // 公共参数
            //parameters["SignatureMethod"] = "HMAC-SHA1";
            //parameters["Format"] = "JSON";
            //parameters["Version"] = "2015-01-09";
            //parameters["SignatureVersion"] = "1.0";
            //parameters["AccessKeyId"] = accessKeyID;
            //parameters["Signature"] = GetRPCSignature(parameters, "GET", accessSecret);
            //parameters["Timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            //parameters["SignatureNonce"] = Convert.ToInt64(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000).ToString();


            // 公共参数
            parameters["SignatureMethod"] = "HMAC-SHA1";
            parameters["SignatureNonce"] = Convert.ToInt64(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000).ToString();
            parameters["AccessKeyId"] = accessKeyID;
            parameters["SignatureVersion"] = "1.0";
            parameters["Timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            parameters["Format"] = "JSON";
            parameters["Version"] = "2015-01-09";
            parameters["Signature"] = GetRPCSignature(parameters, "GET", accessSecret);


            var query = new StringBuilder();
            foreach (var param in parameters)
            {
                query.Append($"{param.Key}={param.Value}&");
            }
            query.Length--; // Remove the last '&'

            return query;
            static string GetRPCSignature(Dictionary<string, string> signedParams, string method, string secret)
            {
                List<string> sortedKeys = signedParams.Keys.ToList();
                sortedKeys.Sort(StringComparer.Ordinal);
                StringBuilder canonicalizedQueryString = new StringBuilder();

                foreach (string key in sortedKeys)
                {
                    if (signedParams[key] != null)
                    {
                        canonicalizedQueryString
                            .Append("&")
                            .Append(PercentEncode(key))
                            .Append("=")
                            .Append(PercentEncode(signedParams[key]));
                    }
                }

                StringBuilder stringToSign = new StringBuilder();
                stringToSign.Append(method);
                stringToSign.Append("&");
                stringToSign.Append(PercentEncode("/"));
                stringToSign.Append("&");
                stringToSign.Append(PercentEncode(canonicalizedQueryString.ToString().Substring(1)));
                byte[] signData;
                using (KeyedHashAlgorithm algorithm = CryptoConfig.CreateFromName("HMACSHA1") as KeyedHashAlgorithm)
                {
                    algorithm.Key = Encoding.UTF8.GetBytes(secret + "&");
                    signData = algorithm.ComputeHash(Encoding.UTF8.GetBytes(stringToSign.ToString().ToCharArray()));
                }

                string signedStr = Convert.ToBase64String(signData);
                return signedStr;

                /// <summary>
                ///     编码
                /// </summary>
                /// <param name="value"></param>
                /// <returns></returns>
                static string PercentEncode(string value)
                {
                    if (value == null)
                        return null;
                    var stringBuilder = new StringBuilder();
                    var str = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
                    foreach (char ch in Encoding.UTF8.GetBytes(value))
                        if (str.IndexOf(ch) >= 0)
                            stringBuilder.Append(ch);
                        else
                            stringBuilder.Append("%").Append(string.Format(CultureInfo.InvariantCulture, "{0:X2}", (int)ch));
                    return stringBuilder.ToString().Replace("+", "%20").Replace("*", "%2A").Replace("%7E", "~");
                }
            }
        }
    }



    #region 响应实体类
    public class AliyunDomainRecordResponse
    {
        public string RequestId { get; set; }

        public string RecordId { get; set; }
    }

    #region 解析记录查询列表响应实体

    public class AliyunRecordItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int TTL { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string RecordId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string RR { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DomainName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Weight { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Line { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Locked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int CreateTimestamp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int UpdateTimestamp { get; set; }
    }

    public class AliyunDomainRecords
    {
        /// <summary>
        /// 
        /// </summary>
        public List<AliyunRecordItem> Record { get; set; }
    }

    public class AliyunDescribeSubDomainRecordsResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int PageSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AliyunDomainRecords DomainRecords { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int PageNumber { get; set; }
    }
    #endregion
    #endregion

    #region 请求实体类

    /// <summary>
    /// 添加域名解析
    /// </summary>
    public class AliyunAddDomainRecordRequest: AliyunBaseRequest
    {
        /// <summary>
        /// 主域名
        /// </summary>
        public string DomainName { get; set; }
        /// <summary>
        /// 前缀 比如子域名 a.test.online  RR=a
        /// </summary>
        public string RR { get; set; }
        /// <summary>
        /// A AAAA
        /// </summary>
        public string Type { get; set; }

        public string TTL { get; set; } = "600";
        /// <summary>
        /// 当前IP
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// 查询解析记录
    /// </summary>
    public class AliyunDescribeSubDomainRecordsRequest: AliyunBaseRequest
    {
       
        /// <summary>
        /// 主域名
        /// </summary>
        public string DomainName { get; set; }
        /// <summary>
        /// 完整二级域名
        /// </summary>
        public string SubDomain { get; set; }
        /// <summary>
        /// A AAAA
        /// </summary>
        public string Type { get; set; }
    }


    /// <summary>
    /// 修改域名解析
    /// </summary>
    public class AliyunUpdateDomainRecordRequest : AliyunBaseRequest
    {
        public string RR { get; set; }
        public string RecordId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string TTL { get; set; }
    }
    public class AliyunBaseRequest
    {
        /// <summary>
        /// 接口名称
        /// </summary>
        public string Action { get; set; }
    }


    #endregion
}
