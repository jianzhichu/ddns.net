namespace ddns.net.model
{
    //[SqlSugar.SugarTable(TableName = "domain_config_info")]
    public class DomainConfigInfo
    {
        //[SqlSugar.SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }
    
        /// <summary>
        /// 主域名
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 二级域名前缀,多个都要隔开
        /// </summary>
        public string SubDomain { get; set; }
        /// <summary>
        /// 解析类型
        /// </summary>
        public string RecordType { get; set; } = "A";

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        //[SqlSugar.SugarColumn(IsNullable =true)]
        public DateTime UpdateTime { get; set; }

       



        /// <summary>
        /// 域名服务商
        /// </summary>
        public string DomainServer { get; set; }

        public string AK { get; set; }
        public string SK { get; set; }

        /// <summary>
        /// 可能是int类型 不能大于86400;
        /// </summary>
        public int Cron { get; set; }
    }
}
