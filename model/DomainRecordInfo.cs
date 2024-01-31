
//using SqlSugar;

namespace ddns.net.model
{
    //[SugarTable(TableName = "domain_record_info")]
    public class DomainRecordInfo
    {

        /// <summary>
        /// Id-->snowId
        /// </summary>
        //[SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }
        /// <summary>
        /// 创建解析时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        //[SugarColumn(IsIgnore =true)]
        public string CreateTimeStr => CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>
        /// IP所在地址
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 电信运营商
        /// </summary>
        //[SugarColumn(IsNullable = true)]
        public string ISP { get; set; }
        /// <summary>
        /// IP
        /// </summary>
        public string Ip { get; set; }

        //[SugarColumn(IsNullable = true)]
        public string LastIp { get; set; }

        /// <summary>
        /// 域名服务商
        /// </summary>
        //[SugarColumn(IsNullable = true)]
        public string Servr { get; set; }
        /// <summary>
        /// 完整域名
        /// </summary>
        //[SugarColumn(IsNullable = true)]
        public string MainDomain { get; set; }
        /// <summary>
        /// 是否已删除-更新配置后，将原有记录改为删除状态
        /// </summary>
        public bool IsDelete { get; set; }

    }
}
