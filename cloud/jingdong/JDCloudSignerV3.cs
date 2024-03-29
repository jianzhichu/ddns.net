﻿using ddns.net.model;
using Quartz.Util;
using System.Globalization;
using System.Text;

namespace ddns.net.cloud.jingdong
{

    /// <summary>
    /// jdcloud signer v3 
    /// </summary>
    public class JDCloudSignerV3
    {

        //private static readonly string[] REQUEST_METHOD = { "GET", "POST", "PUT", "HEAD", "PATCH", "DELETE", "CONNECT", "OPTIONS", "TRACE" };

        private static readonly string[] NOT_SIGN_REQUEST_HEAD = { "cache-control","content-type","content-length",
            "host","expect","max-forwards","pragma","range","te","if-match","if-none-match","if-modified-since","if-unmodified-since","if-range",
            "accept","authorization","proxy-authorization","from","referer","user-agent","x-jdcloud-request-id"};
        private static readonly string[] NOT_SIGN_REQUEST_HEAD_START = { "x-b3-" };
        /// <summary>
        /// signer v3 code
        /// </summary>
        public JDCloudSignerV3()
        {
        }






        /// <summary>
        /// do sign http request message
        /// </summary>
        /// <param name="httpRequestMessage">the http request</param>
        /// <param name="credentials">the jdcloud credentials</param>
        /// <param name="overWriteDate">over write sign data</param>
        /// <param name="serviceName">the current http request request serviceName</param>
        /// <returns></returns>
        public static HttpRequestMessage DoRequestMessageSign( HttpRequestMessage httpRequestMessage, DomainConfigInfo credentials,
            string serviceName)
        {
            var headers = httpRequestMessage.Headers;
            var requestUri = httpRequestMessage.RequestUri;
            var queryString = requestUri.Query;
            var requestPath = requestUri.AbsolutePath;
            var requestContent = httpRequestMessage.Content;
            var requestMethod = httpRequestMessage.Method;
            string apiVersion = "v2";
            RequestModel requestModel = new RequestModel();
            requestModel.ApiVersion = apiVersion;
            if (requestContent != null)
            {
                using (var contentStream = new MemoryStream())
                {
                    requestContent.CopyToAsync(contentStream).Wait();
                    if (contentStream.Length > 0)
                    {
                        requestModel.Content = contentStream.ToArray();
                    }
                }

                requestModel.ContentType = requestContent.Headers.ContentType.ToString();
            }
            requestModel.HttpMethod = requestMethod.ToString().ToUpper();
            var pathRegion = "cn-north-1";
            if (!string.IsNullOrWhiteSpace(pathRegion))
            {
                requestModel.RegionName = pathRegion;
            }
            else
            {
                requestModel.RegionName = ParameterConstant.DEFAULT_REGION;
            }

            requestModel.ResourcePath = requestPath;
                requestModel.ServiceName = serviceName;
            //JDCloudSignVersionType jDCloudSignVersionType = GlobalConfig.GetInstance().SignVersionType;
            //if (signType != null && signType.HasValue)
            //{
            //    jDCloudSignVersionType = signType.Value;
            //}
            requestModel.SignType = JDCloudSignVersionType.JDCloud_V3;
            requestModel.Uri = requestUri;
            requestModel.QueryParameters = queryString;
            //requestModel.OverrddenDate = overWriteDate;

            if (!(requestUri.Scheme.ToLower() == "http" && requestUri.Port == 80) &&
                !(requestUri.Scheme.ToLower() == "https" && requestUri.Port == 443))
            {
                requestModel.RequestPort = requestUri.Port;
            }
            foreach (var headerKeyValue in headers)
            {
                requestModel.AddHeader(headerKeyValue.Key, string.Join(",", headerKeyValue.Value));
            }
            SignedRequestModel signedRequestModel = Sign(requestModel, credentials);
            var signedHeader = signedRequestModel.RequestHead;
            foreach (var key in signedHeader.Keys)
            {
                if (!httpRequestMessage.Headers.Contains(key))
                {
                    var value = signedHeader[key];
                    httpRequestMessage.Headers.TryAddWithoutValidation(key, value);
                }
            }
            return httpRequestMessage;
        }



        /// <summary>
        /// sign with RequestModel
        /// </summary>
        /// <param name="requestModel"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static SignedRequestModel Sign(RequestModel requestModel, DomainConfigInfo config)
        {

            string nonceId = "";
            var requestHeader = ProcessRequestHeaderKeyToLower(requestModel.Header);
            if (requestModel.NonceId.IsNullOrWhiteSpace())
            {
                nonceId = Guid.NewGuid().ToString().ToLower();
            }
            //else if (requestHeader != null &&
            //    requestHeader.Count > 0 &&
            //    requestHeader.ContainsKey(ParameterConstant.X_JDCLOUD_NONCE.ToLower()))
            //{
            //    List<string> headValues = requestHeader[ParameterConstant.X_JDCLOUD_NONCE.ToLower()];
            //    if (headValues != null && headValues.Count > 0)
            //    {
            //        nonceId = headValues[0];
            //    }
            //    else
            //    {
            //        nonceId = Guid.NewGuid().ToString().ToLower();
            //    }
            //}
            //else
            //{
            //    nonceId = requestModel.NonceId;
            //}

            DateTime? signDate = DateTime.UtcNow;
            //if (requestHeader != null &&
            //    requestHeader.Count > 0 &&
            //    requestHeader.ContainsKey(ParameterConstant.X_JDCLOUD_DATE.ToLower()))
            //{
            //    List<string> headerValues = requestHeader[ParameterConstant.X_JDCLOUD_DATE.ToLower()];
            //    if (headerValues != null && headerValues.Count > 0)
            //    {
            //        string dateString = headerValues[0];
            //        if (!dateString.IsNullOrWhiteSpace())
            //        {
            //            var tryParseDate = DateTime.Now;
            //            if (DateTime.TryParseExact(dateString, ParameterConstant.DATA_TIME_FORMAT,
            //                CultureInfo.GetCultureInfo("en-US"), System.Globalization.DateTimeStyles.None,
            //                       out tryParseDate))
            //            {
            //                signDate = tryParseDate;
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //    if (requestModel.OverrddenDate != null && requestModel.OverrddenDate.HasValue)
            //    {
            //        signDate = requestModel.OverrddenDate.Value;
            //    }
            //}
            //if (signDate == null || !signDate.HasValue)
            //{
            //    signDate = DateTime.UtcNow;
            //}
            string formattedSigningDateTime = signDate.Value.ToString(ParameterConstant.DATA_TIME_FORMAT);
            string formattedSigningDate = signDate.Value.ToString(ParameterConstant.HEADER_DATA_FORMAT);
            string scope = SignUtil.GenerateScope(formattedSigningDate, requestModel.ServiceName, requestModel.RegionName, ParameterConstant.JDCLOUD_TERMINATOR_V3);

            requestHeader.Add(ParameterConstant.X_JDCLOUD_DATE,
                              new List<string> { formattedSigningDateTime });
            requestHeader.Add(ParameterConstant.X_JDCLOUD_NONCE,
                              new List<string> { nonceId });
            if (requestHeader.ContainsKey(ParameterConstant.X_JDCLOUD_ALGORITHM.ToLower()))
            {
                requestHeader[ParameterConstant.X_JDCLOUD_ALGORITHM.ToLower()] = new List<string> { ParameterConstant.JDCLOUD3_SIGNING_ALGORITHM_V3 };
            }
            else
            {
                requestHeader.Add(ParameterConstant.X_JDCLOUD_ALGORITHM.ToLower(), new List<string> { ParameterConstant.JDCLOUD3_SIGNING_ALGORITHM_V3 });
            }
            var contentSHA256 = "";
            if (requestHeader.ContainsKey(ParameterConstant.X_JDCLOUD_CONTENT_SHA256))
            {
                List<string> contentSha256Value = requestHeader[ParameterConstant.X_JDCLOUD_CONTENT_SHA256];
                if (contentSha256Value != null && contentSha256Value.Count > 0)
                {
                    contentSHA256 = contentSha256Value[0];
                }

            }
            if (contentSHA256.IsNullOrWhiteSpace())
            {
                contentSHA256 = SignUtil.CalculateContentHash(requestModel.Content);
            }
            string queryParams = requestModel.QueryParameters;// ProcessQueryString(requestModel.QueryParameters);
            string requestPath = requestModel.ResourcePath;// ProcessRequestPath(requestModel.ResourcePath);
            string requestMethod = requestModel.HttpMethod.ToUpper();
            Dictionary<string, string> processHeader = ProcessRequstHeader(ProcessRequestHeaderWithMoreValue(requestModel.Header));
            string signHeaderString = GetSignedHeadersString(processHeader);
            string signHeaderKeyString = GetSignedHeadersKeyString(processHeader);
            var canonicalRequest = SignUtil.CreateCanonicalRequest(queryParams, requestPath, requestMethod, signHeaderString, signHeaderKeyString, contentSHA256);
            var stringToSign = SignUtil.CreateStringToSign(canonicalRequest, formattedSigningDateTime, scope, ParameterConstant.JDCLOUD3_SIGNING_ALGORITHM_V3);

            byte[] kSecret = System.Text.Encoding.UTF8.GetBytes($"JDCLOUD3{config.SK}");
            byte[] kDate = SignUtil.Sign(formattedSigningDate, kSecret, ParameterConstant.SIGN_SHA256);
            byte[] kRegion = SignUtil.Sign(requestModel.RegionName, kDate, ParameterConstant.SIGN_SHA256);
            byte[] kService = SignUtil.Sign(requestModel.ServiceName, kRegion, ParameterConstant.SIGN_SHA256);
            byte[] signingKey = SignUtil.Sign(ParameterConstant.JDCLOUD_TERMINATOR_V3, kService, ParameterConstant.SIGN_SHA256);
            byte[] signature = SignUtil.ComputeSignature(stringToSign, signingKey);
            // Console.WriteLine($" kSecret={ BitConverter.ToString(kSecret).Replace("-", "")}");
            // Console.WriteLine($" kDate={ BitConverter.ToString(kDate).Replace("-", "")}");
            // Console.WriteLine($" kRegion={ BitConverter.ToString(kRegion).Replace("-", "")}");
            // Console.WriteLine($" kService={ BitConverter.ToString(kService).Replace("-", "")}");
            // Console.WriteLine($" signingKey={ BitConverter.ToString(signingKey).Replace("-", "")}");
            // Console.WriteLine($" signature={ BitConverter.ToString(signature).Replace("-", "")}");

            string signingCredentials = config.AK + "/" + scope;
            string credential = "Credential=" + signingCredentials;
            string signerHeaders = "SignedHeaders=" + signHeaderKeyString;
            string signatureHeader = "Signature=" + SignUtil.ByteToHex(signature, true);

            var signHeader = new StringBuilder().Append(ParameterConstant.JDCLOUD3_SIGNING_ALGORITHM_V3)
                    .Append(" ")
                    .Append(credential)
                    .Append(", ")
                    .Append(signerHeaders)
                    .Append(", ")
                    .Append(signatureHeader)
                    .ToString();

            requestModel.AddHeader(ParameterConstant.AUTHORIZATION, signHeader);
            SignedRequestModel signedRequestModel = new SignedRequestModel();
            signedRequestModel.CanonicalRequest = canonicalRequest;
            signedRequestModel.ContentSHA256 = contentSHA256;
            foreach (var header in requestModel.Header)
            {
                signedRequestModel.RequestHead.Add(header.Key, string.Join(",", header.Value.ToArray()));
            }
            signedRequestModel.RequestNonceId = nonceId;
            signedRequestModel.SignedHeaders = signHeader;
            signedRequestModel.StringSignature = stringToSign;
            signedRequestModel.StringToSign = stringToSign;

            return signedRequestModel;
        }




        //private static string ProcessRequestMethod(string requestMethod)
        //{

        //    if (requestMethod.IsNullOrWhiteSpace())
        //    {
        //        return requestMethod;
        //    }

        //    requestMethod = requestMethod.ToUpper();

        //    if (!REQUEST_METHOD.Contains(requestMethod))
        //    {
        //        throw new ArgumentException($" request method :{requestMethod} not support");
        //    }


        //    return requestMethod;
        //}



        /// <summary>
        /// get header signed string
        /// </summary>
        /// <param name="needSignHeaders">the headers while sign</param>
        /// <returns></returns>

        public static string GetSignedHeadersString(IDictionary<string, string> needSignHeaders)
        {
            if (needSignHeaders == null | needSignHeaders.Count == 0)
            {
                return null;
            }

            needSignHeaders = needSignHeaders.OrderBy(p => p.Key).ToDictionary(k => k.Key, v => v.Value);

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in needSignHeaders)
            {
                string key = item.Key.ToLower(CultureInfo.GetCultureInfo("en-US"));
                stringBuilder.Append(key).Append(":").Append(item.Value).Append("\n");
            }
            return stringBuilder.ToString();

        }

        /// <summary>
        /// get need sign header key 
        /// </summary>
        /// <param name="needSignHeaders"></param>
        /// <returns></returns>
        public static string GetSignedHeadersKeyString(Dictionary<string, string> needSignHeaders)
        {
            if (needSignHeaders == null | needSignHeaders.Count == 0)
            {
                return null;
            }
            needSignHeaders = needSignHeaders.OrderBy(p => p.Key).ToDictionary(k => k.Key, v => v.Value);
            string[] signHeaderKey = needSignHeaders.Keys.ToArray();

            string signHeader = string.Join(";", signHeaderKey);
            return signHeader;
        }

        /// <summary>
        /// process request header key to lower
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static Dictionary<string, List<string>> ProcessRequestHeaderKeyToLower(Dictionary<string, List<string>> header)
        {
            if (header == null)
            {
                return null;
            }
            else if (header.Count == 0)
            {
                return header;
            }
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            foreach (var item in header)
            {
                if (result.ContainsKey(item.Key.ToLower()))
                {
                    if (result[item.Key.ToLower()] != item.Value)
                    {
                        result[item.Key.ToLower()].AddRange(item.Value);
                    }
                }
                else
                {
                    result.Add(item.Key.ToLower(), item.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// convert header more then one value to a string value
        /// </summary>
        /// <param name="header"> the request header </param>
        /// <returns></returns>
        public static Dictionary<string, string> ProcessRequestHeaderWithMoreValue(Dictionary<string, List<string>> header)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (header != null && header.Count > 0)
            {
                foreach (var item in header)
                {
                    if (!item.Key.IsNullOrWhiteSpace())
                    {
                        if (item.Value != null && item.Value.Count > 0)
                        {
                            string value = string.Join(",", item.Value.Select(p => p.Trim()).ToArray());

                            result.Add(item.Key, value);
                        }
                        else
                        {
                            result.Add(item.Key, string.Empty);
                        }

                    }
                }
            }
            return result;

        }

        /// <summary>
        /// process request head info
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ProcessRequstHeader(Dictionary<string, string> header)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (header != null && header.Count > 0)
            {

                foreach (var item in header)
                {
                    if (!item.Key.IsNullOrWhiteSpace())
                    {
                        string key = item.Key.ToLower().Trim();
                        if (!NOT_SIGN_REQUEST_HEAD.Contains(key) &&
                            !NOT_SIGN_REQUEST_HEAD_START.Any(p => key.Trim().StartsWith(p)))
                        {
                            result.Add(key, item.Value.Trim());
                        }

                    }
                }
            }

            return result;
        }


        /// <summary>
        /// process requst url query string value
        /// </summary>
        /// <param name="queryString">request query string </param>
        /// <returns></returns>
        //public static string ProcessQueryString(string queryString)
        //{

        //    if (queryString.IsNullOrWhiteSpace())
        //    {

        //        return string.Empty;
        //    }
        //    if (queryString.StartsWith("?"))
        //    {
        //        queryString = queryString.Remove(0, 1);
        //    }

        //    var queryStringArray = queryString.Split('&');
        //    List<QueryParam> queryParamList = new List<QueryParam>();
        //    if (queryStringArray != null && queryStringArray.Length > 0)
        //    {
        //        foreach (var item in queryStringArray)
        //        {
        //            if (!item.IsNullOrWhiteSpace())
        //            {
        //                var paramKV = item.Split('=');
        //                if (paramKV != null && paramKV.Length > 0)
        //                {
        //                    if (paramKV.Length == 1 && !paramKV[0].IsNullOrWhiteSpace())
        //                    {
        //                        // queryString = queryString.Replace("+", " ");
        //                        string keyValue = paramKV[0].Replace("+", " ");
        //                        keyValue = JDCloudSignV3Util.UnescapeDataStringRfc3986(keyValue);
        //                        QueryParam queryParam = new QueryParam { Key = JDCloudSignV3Util.EscapeUriDataStringRfc3986(keyValue), Value = string.Empty };
        //                        queryParamList.Add(queryParam);
        //                    }
        //                    else if (paramKV.Length == 2 && !paramKV[0].IsNullOrWhiteSpace())
        //                    {
        //                        string keyValue = paramKV[0].Replace("+", " ");
        //                        keyValue = JDCloudSignV3Util.UnescapeDataStringRfc3986(keyValue);
        //                        string value = paramKV[1].Replace("+", " ");
        //                        value = JDCloudSignV3Util.UnescapeDataStringRfc3986(value);
        //                        QueryParam queryParam = new QueryParam
        //                        {
        //                            Key = JDCloudSignV3Util.EscapeUriDataStringRfc3986(keyValue),
        //                            Value = JDCloudSignV3Util.EscapeUriDataStringRfc3986(value)
        //                        };
        //                        queryParamList.Add(queryParam);
        //                    }
        //                    else
        //                    {
        //                        if (paramKV[0] != null && paramKV[0] != string.Empty)
        //                        {

        //                            string key = JDCloudSignV3Util.UnescapeDataStringRfc3986(paramKV[0]);

        //                            string queryParamValue = string.Join("=", paramKV.Skip(1).Take(paramKV.Length - 1).ToArray());

        //                            string value = JDCloudSignV3Util.UnescapeDataStringRfc3986(queryParamValue);
        //                            QueryParam queryParam = new QueryParam
        //                            {
        //                                Key = JDCloudSignV3Util.EscapeUriDataStringRfc3986(key),
        //                                Value = JDCloudSignV3Util.EscapeUriDataStringRfc3986(value)
        //                            };
        //                            queryParamList.Add(queryParam);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (queryParamList.Count > 0)
        //    {
        //        queryParamList = queryParamList.OrderBy(p => p.Key).ThenBy(p => p.Value).ToList();

        //        StringBuilder stringBuilder = new StringBuilder();
        //        foreach (var item in queryParamList)
        //        {
        //            stringBuilder.Append(item.Key).Append("=");
        //            if (item.Value.IsNullOrWhiteSpace())
        //            {
        //                stringBuilder.Append(string.Empty).Append("&");
        //            }
        //            else
        //            {
        //                stringBuilder.Append(item.Value).Append("&");
        //            }
        //        }
        //        var queryStringEncode = stringBuilder.ToString();
        //        if (!queryStringEncode.IsNullOrWhiteSpace())
        //        {
        //            if (queryStringEncode.EndsWith("&"))
        //            {
        //                queryStringEncode = queryStringEncode.TrimEnd('&');
        //            }
        //        }
        //        else
        //        {
        //            queryStringEncode = string.Empty;
        //        }

        //        return queryStringEncode;
        //    }
        //    return string.Empty;
        //}

        //private static string ProcessRequestPath(string requestPath)
        //{
        //    var path = requestPath.Replace("+", " ");
        //    var decodePath = JDCloudSignV3Util.UnescapeDataStringRfc3986(path);
        //    var pathArray = decodePath.Split('/');

        //    var pathList = pathArray.Select(p => JDCloudSignV3Util.EscapeUriDataStringRfc3986(p)).ToList();
        //    pathList.RemoveAll(p => p.Equals(string.Empty));
        //    path = string.Join("/", pathList.ToArray());

        //    pathList.RemoveAll(p => p.Equals(string.Empty));

        //    path = string.Join("/", pathList.ToArray());
        //    if (!path.StartsWith("/"))
        //    {
        //        path = $"/{path}";
        //    }
        //    if (requestPath.TrimEnd().EndsWith("/") || requestPath.TrimEnd().EndsWith("%2F"))
        //    {
        //        path = $"{path}/";
        //    }
        //    return path;
        //}
    }

    /// <summary>
    /// model use save query param
    /// </summary>
    internal class QueryParam
    {
        /// <summary>
        /// query string key
        /// </summary>
        public string Key { get; set; }


        /// <summary>
        /// query string value
        /// </summary>
        public string Value { get; set; }
    }
}
