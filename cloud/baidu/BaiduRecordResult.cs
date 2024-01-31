namespace ddns.net.cloud.baidu
{


    public class BaiduRecord
    {
        /// <summary>
        /// 
        /// </summary>
        public int recordId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string domain { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string view { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rdtype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ttl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rdata { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string zoneName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
    }

    public class BaiduRecordResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string orderBy { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string order { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pageNo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pageSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int totalCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<BaiduRecord> result { get; set; }
    }

}
