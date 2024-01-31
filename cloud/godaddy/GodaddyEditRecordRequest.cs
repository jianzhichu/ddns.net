namespace ddns.net.cloud.godaddy
{
    public class GodaddyEditRecordRequest
    {
        public string domain { get; set; }
        public string name { get; set; }
        public string type { get; set; }

        public List<GodaddyEditRecordItem> records { get; set; }
    }


    public class GodaddyEditRecordItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string data { get; set; }
    }
}
