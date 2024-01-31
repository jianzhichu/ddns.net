namespace ddns.net.cloud.baidu
{
    public class BaiduAddRecordRequest
    {
        /// <summary>
        /// rr主机名称，例如www或*或@，非完整域名
        /// </summary>
        public string domain { get; set; }
        //public string view { get; set; }
        /// <summary>
        /// 解析记录类型
        /// </summary>
        public string rdType { get; set; }

        /// <summary>
        /// 指向值
        /// </summary>
        public string rdata { get; set; }

        /// <summary>
        /// 主域名
        /// </summary>
        public string zoneName { get; set; }
    }
}
