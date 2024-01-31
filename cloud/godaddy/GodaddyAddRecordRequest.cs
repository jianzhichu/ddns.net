namespace ddns.net.cloud.godaddy
{
    public class GodaddyAddRecordRequest
    {
        public string domain { get; set; }

        public List<GodaddyAddRecordItem> records { get; set; }
    }


    public class GodaddyAddRecordItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
    }
}
