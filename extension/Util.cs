using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ddns.net.extension
{
    public static class Util
    {

        public static string JWT_TOKEN_KEY = "ddns.net-key-" + Guid.NewGuid().ToString();
        public static string JWT_TOKEN_KEY_DEV = "ddns.net-key-" + DateTime.Now.ToString("yyyy");
        public static string Md5(this string inputString)
        {

            // 将输入字符串转换为字节数组
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);

            // 创建一个 MD5 对象
            using MD5 md5 = MD5.Create();
            // 计算哈希值
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // 将哈希值转换为十六进制字符串
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            string hashString = sb.ToString();
            return hashString;
        }

        public static string UPLOAD_PATH_PRO = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "upload");
        public static string UPLOAD_PATH_DEV = Path.Combine(Directory.GetCurrentDirectory(), "upload");


        /// <summary>
        /// 单层实体类对象转字典
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Dictionary<string, string> SimpleObjConvertToDic<T>(this T entity) where T: class
        {

            var dictionary = new Dictionary<string, string>();
            if(entity != null)
            {
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);
                    if (value != null)
                        dictionary.Add(property.Name, value.ToString());
                }
            }

            return dictionary;
        }


        public static string SHA256Hex(string s)
        {
            using (SHA256 algo = SHA256.Create())
            {
                byte[] hashbytes = algo.ComputeHash(Encoding.UTF8.GetBytes(s));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashbytes.Length; ++i)
                {
                    builder.Append(hashbytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static byte[] HmacSHA256(byte[] key, byte[] msg)
        {
            using (HMACSHA256 mac = new HMACSHA256(key))
            {
                return mac.ComputeHash(msg);
            }
        }

        public static long ToTimestamp()
        {
            DateTimeOffset expiresAtOffset = DateTimeOffset.UtcNow;
            var totalSeconds = expiresAtOffset.ToUnixTimeMilliseconds();
            return totalSeconds;
        }
    }


    /// <summary>
    /// 域名服务商
    /// </summary>
    public enum DomainServer
    {
        Aliyun,
        Baidu,
        Tencent,
        Huawei,
        Jingdong,
        West,
        Godaddy
    }
}
