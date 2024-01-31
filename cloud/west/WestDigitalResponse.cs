namespace ddns.net.cloud.west
{

    //如果好用，请收藏地址，帮忙分享。
    public class WestDigitalItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int record_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string hostname { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string record_value { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string record_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int record_mx { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int record_ttl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string record_line { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pause { get; set; }
    }

    public class WestItemResponseBody
    {
        /// <summary>
        /// 
        /// </summary>
        public int pageno { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int limit { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pagecount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<WestDigitalItem> items { get; set; }
    }

    public class WestDigitalResponse<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string msg { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public T body { get; set; }
    }




    public class WestDigitalAddRecord
    {

        public int record_id { get; set; }
    }



    public class WestDigitalEditRecord
    {
        /// <summary>
        /// 
        /// </summary>
        public List<int> success { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<int> fail { get; set; }
    }
}
