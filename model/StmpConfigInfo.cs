namespace ddns.net.model
{
    //[SqlSugar.SugarTable(TableName = "stmp_config_info")]
    public class StmpConfigInfo
    {

       // [SqlSugar.SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }
        public string Stmp { get; set; }
        public int   Port { get; set; }
        public string Code { get; set; }
        public string From { get; set; }

        /// <summary>
        /// 
        /// </summary>
       // [SqlSugar.SugarColumn(IsNullable =true)]
        public string To { get; set; }

        /// <summary>
        /// 是否开启发送邮件提醒
        /// </summary>
        public bool Open { get; set; }
    }

}
