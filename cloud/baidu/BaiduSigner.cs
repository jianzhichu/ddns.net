using System.Security.Cryptography;
using System.Text;

namespace ddns.net.cloud.baidu
{
    public class BaiduSigner
    {
        //private const string BaiduDateFormat = "yyyy-MM-ddTHH:mm:ssZ";
        //private const string ExpirationPeriod = "1800";
        //private const string HOST = "bcd.baidubce.com";


        /// <summary>
        /// 生成请求头
        /// </summary>
        /// <param name="AK"></param>
        /// <param name="SK"></param>
        /// <param name="method"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetAuthHeaders(string AK,string SK,HttpMethod method, string api)
        {
            string HOST = "bcd.baidubce.com";
            Dictionary<string, string> headers = new();
            var time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string authStringPrefix = $"bce-auth-v1/{AK}/{time}/{1800}";
            string baiduCanonicalURL = BaiduCanonicalURI(api);
            string canonicalReq = $"{method.ToString().ToUpper()}\n{baiduCanonicalURL}\n\nhost:{HOST}";
            string signingKey = HmacSha256Hex(SK, authStringPrefix);
            string signature = HmacSha256Hex(signingKey, canonicalReq);
            string authString = $"{authStringPrefix}/host/{signature}";

            headers.Add("authorization", authString);
            headers.Add("host", HOST);
            headers.Add("x-bce-date", time);
            //headers.Add("content-type", "application/json; charset=utf-8");
            return headers;
        }

        static string HmacSha256Hex(string secret, string message)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            using (var hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        static string BaiduCanonicalURI(string request)
        {
            string[] patterns = request.Split('/');
            var uri = new string[patterns.Length];
            for (int i = 0; i < patterns.Length; i++)
            {
                uri[i] = Escape(patterns[i]);
            }
            string urlpath = string.Join("/", uri);
            if (urlpath.Length == 0 || urlpath[urlpath.Length - 1] != '/')
            {
                urlpath += "/";
            }
            return urlpath.Substring(0, urlpath.Length - 1);
        }



        static string Escape(string input)
        {
            // Implement your own URL escaping logic here
            return Uri.EscapeDataString(input);
        }
    }
}
