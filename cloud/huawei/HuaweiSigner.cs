using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ddns.net.cloud.huawei
{
    public  class HuaweiSigner
    {
        const string BasicDateFormat = "yyyyMMddTHHmmssZ";
        const string Algorithm = "SDK-HMAC-SHA256";
        const string HeaderXDate = "X-Sdk-Date";
        const string HeaderHost = "host";
        const string HeaderAuthorization = "Authorization";
        const string HeaderContentSha256 = "X-Sdk-Content-Sha256";
        readonly HashSet<string> unsignedHeaders = new HashSet<string> { "content-type" };




       /// <summary>
       /// 
       /// </summary>
       /// <param name="client"></param>
       /// <param name="reqbody"></param>
       /// <param name="url"></param>
       /// <param name="AK"></param>
       /// <param name="SK"></param>
        public void Sign(HttpClient client, string reqbody, string url, string AK, string SK)
        {
            var host = "dns.myhuaweicloud.com"; 
            client.BaseAddress = new Uri(host);

            HuaweiHttpRequest r = new HuaweiHttpRequest("POST", new Uri($"https://{host}/{url}"))
            {
                body = reqbody
            };
            r.headers.Add("Content-Type", "application/json");

            var t = DateTime.Now;
            r.headers.Add(HeaderXDate, t.ToUniversalTime().ToString(BasicDateFormat));
            var signedHeaders = SignedHeaders(r);
            var canonicalRequest = CanonicalRequest(r, signedHeaders);
            //r.canonicalRequest = canonicalRequest;
            var stringToSign = StringToSign(canonicalRequest, t);
            //r.stringToSign = stringToSign;
            var signature = SignStringToSign(stringToSign, Encoding.UTF8.GetBytes(SK));
            var authValue = AuthHeaderValue(signature, signedHeaders,AK);

            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderHost,host );
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderAuthorization, authValue);
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderXDate, t.ToUniversalTime().ToString(BasicDateFormat));
        }


        byte[] hmacsha256(byte[] keyByte, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                return hmacsha256.ComputeHash(messageBytes);
            }
        }

        private string CanonicalRequest(HuaweiHttpRequest r, List<string> signedHeaders)
        {
            string hexencode;
            if (r.headers.Get(HeaderContentSha256) != null)
            {
                hexencode = r.headers.Get(HeaderContentSha256);
            }
            else
            {
                var data = Encoding.UTF8.GetBytes(r.body);
                hexencode = HexEncodeSHA256Hash(data);
            }
            return string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}", r.method, CanonicalURI(r), CanonicalQueryString(r), CanonicalHeaders(r, signedHeaders), string.Join(";", signedHeaders), hexencode);
        }
        string CanonicalURI(HuaweiHttpRequest r)
        {
            var pattens = r.uri.Split('/');
            List<string> uri = new List<string>();
            foreach (var v in pattens)
            {
                uri.Add(UrlEncode(v));
            }
            var urlpath = string.Join("/", uri);
            if (urlpath[urlpath.Length - 1] != '/')
            {
                urlpath = urlpath + "/"; // always end with /
            }
            //r.uri = urlpath;
            return urlpath;
        }
        string CanonicalQueryString(HuaweiHttpRequest r)
        {
            List<string> keys = new List<string>();
            foreach (var pair in r.query)
            {
                keys.Add(pair.Key);
            }
            keys.Sort(String.CompareOrdinal);
            List<string> a = new List<string>();
            foreach (var key in keys)
            {
                string k = UrlEncode(key);
                List<string> values = r.query[key];
                values.Sort(String.CompareOrdinal);
                foreach (var value in values)
                {
                    string kv = k + "=" + UrlEncode(value);
                    a.Add(kv);
                }
            }
            return string.Join("&", a);
        }
        string CanonicalHeaders(HuaweiHttpRequest r, List<string> signedHeaders)
        {
            List<string> a = new List<string>();
            foreach (string key in signedHeaders)
            {
                var values = new List<string>(r.headers.GetValues(key));
                values.Sort(String.CompareOrdinal);
                foreach (var value in values)
                {
                    a.Add(key + ":" + value.Trim());
                    r.headers.Set(key, Encoding.GetEncoding("iso-8859-1").GetString(Encoding.UTF8.GetBytes(value)));
                }
            }
            return string.Join("\n", a) + "\n";
        }
        public List<string> SignedHeaders(HuaweiHttpRequest r)
        {
            List<string> a = new List<string>();
            foreach (string key in r.headers.AllKeys)
            {
                string keyLower = key.ToLower();
                if (!unsignedHeaders.Contains(keyLower))
                {
                    a.Add(key.ToLower());
                }
            }
            a.Sort(String.CompareOrdinal);
            return a;
        }

        static char GetHexValue(int i)
        {
            if (i < 10)
            {
                return (char)(i + '0');
            }
            return (char)(i - 10 + 'a');
        }
        private static string toHexString(byte[] value)
        {
            int num = value.Length * 2;
            char[] array = new char[num];
            int num2 = 0;
            for (int i = 0; i < num; i += 2)
            {
                byte b = value[num2++];
                array[i] = GetHexValue(b / 16);
                array[i + 1] = GetHexValue(b % 16);
            }
            return new string(array, 0, num);
        }
        // Create a "String to Sign".
        private string StringToSign(string canonicalRequest, DateTime t)
        {
            SHA256 sha256 = new SHA256Managed();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest));
            sha256.Clear();
            return string.Format("{0}\n{1}\n{2}", Algorithm, t.ToUniversalTime().ToString(BasicDateFormat), toHexString(bytes));
        }


        // Create the HWS Signature.
        private string SignStringToSign(string stringToSign, byte[] signingKey)
        {
            byte[] hm = hmacsha256(signingKey, stringToSign);
            return toHexString(hm);
        }
        // HexEncodeSHA256Hash returns hexcode of sha256
        private static string HexEncodeSHA256Hash(byte[] body)
        {
            SHA256 sha256 = new SHA256Managed();
            var bytes = sha256.ComputeHash(body);
            sha256.Clear();
            return toHexString(bytes);
        }
        // Get the finalized value for the "Authorization" header. The signature parameter is the output from SignStringToSign
        private string AuthHeaderValue(string signature, List<string> signedHeaders,string AK)
        {
            return string.Format("{0} Access={1}, SignedHeaders={2}, Signature={3}", Algorithm, AK, string.Join(";", signedHeaders), signature);
        }

        private static char IntToHex(int n)
        {
            if (n <= 9)
                return (char)(n + (int)'0');
            else
                return (char)(n - 10 + (int)'A');
        }

        private static bool IsUrlSafeChar(char ch)
        {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                return true;

            switch (ch)
            {
                case '-':
                case '_':
                case '.':
                case '~':
                    return true;
            }

            return false;
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            int cUnsafe = 0;

            // count them first
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];

                if (!IsUrlSafeChar(ch))
                    cUnsafe++;
            }

            // nothing to expand?
            if (cUnsafe == 0)
            {
                // DevDiv 912606: respect "offset" and "count"
                if (0 == offset && bytes.Length == count)
                {
                    return bytes;
                }
                else
                {
                    var subarray = new byte[count];
                    Buffer.BlockCopy(bytes, offset, subarray, 0, count);
                    return subarray;
                }
            }

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cUnsafe * 2];
            int pos = 0;

            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                char ch = (char)b;

                if (IsUrlSafeChar(ch))
                {
                    expandedBytes[pos++] = b;
                }
                else
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            }

            return expandedBytes;
        }
        private static string UrlEncode(string value)
        {
            if (value == null)
                return null;

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Encoding.UTF8.GetString(UrlEncode(bytes, 0, bytes.Length));
        }

    }



    public class HuaweiHttpRequest
    {
        public string method;
        public string host; /*   http://example.com  */
        public string uri = "/";  /*   /request/uri      */
        public Dictionary<string, List<string>> query = new Dictionary<string, List<string>>();
        public WebHeaderCollection headers = new WebHeaderCollection();
        public string body = "";
        public string canonicalRequest;
        public string stringToSign;
        public HuaweiHttpRequest(string method = "GET", Uri url = null, WebHeaderCollection headers = null, string body = null)
        {
            if (method != null)
            {
                this.method = method;
            }
            if (url != null)
            {
                host = url.Scheme + "://" + url.Host + ":" + url.Port;
                uri = url.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
                query = new Dictionary<string, List<string>>();
                if (url.Query.Length > 1)
                {
                    foreach (var kv in url.Query.Substring(1).Split('&'))
                    {
                        string[] spl = kv.Split(new char[] { '=' }, 2);
                        string key = Uri.UnescapeDataString(spl[0]);
                        string value = "";
                        if (spl.Length > 1)
                        {
                            value = Uri.UnescapeDataString(spl[1]);
                        }
                        if (query.ContainsKey(key))
                        {
                            query[key].Add(value);
                        }
                        else
                        {
                            query[key] = new List<string> { value };
                        }
                    }
                }
            }
            if (headers != null)
            {
                this.headers = headers;
            }
            if (body != null)
            {
                this.body = body;
            }
        }
    }
}
