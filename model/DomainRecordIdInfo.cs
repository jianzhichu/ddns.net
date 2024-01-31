namespace ddns.net.model
{
    //[SqlSugar.SugarTable(TableName = "domain_record_id_info")]
    public class DomainRecordIdInfo
    {
        //[SqlSugar.SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        /// <summary>
        /// 主域名
        /// </summary>
        //[SqlSugar.SugarColumn(IsNullable = true)]
        public string Domain { get; set; }

        /// <summary>
        /// 二级域名前缀
        /// </summary>
       // [SqlSugar.SugarColumn(IsNullable = true)]
        public string SubDomain { get; set; }

        /// <summary>
        /// 解析记录Id
        /// </summary>
       // [SqlSugar.SugarColumn(IsNullable = true)]
        public string RecodeId { get; set; }
        /// <summary>
        /// 华为云特有
        /// </summary>
      //  [SqlSugar.SugarColumn(IsNullable = true)]
        public string ZoneId { get; set; }

        /// <summary>
        /// 京东云特有
        /// </summary>
      // [SqlSugar.SugarColumn(IsNullable = true)]
        public string DomainId { get; set; }

        // [SqlSugar.SugarColumn(IsNullable = true)]
        public string Server { get; set; }

        public string RecordType { get; set; }
    }
}
