﻿//using SqlSugar;

namespace ddns.net.dto
{
    public class InitSystemInfo
    {
        public int DbType { get; set; }

        public string ConnString { get; set; }

        public string UserName { get; set; }

        public string UswePwd { get; set; }

        /// <summary>
        /// 主域名
        /// </summary>
        public string domainName { get; set; }
        /// <summary>
        /// 二级域名前缀数组
        /// </summary>
        public string domainRecord { get; set; }
        /// <summary>
        /// 解析类型
        /// </summary>
        public string recordType { get; set; } 

        /// <summary>
        /// 域名服务商
        /// </summary>
        public string cloudName { get; set; }

        public string AK { get; set; }
        public string SK { get; set; }

        /// <summary>
        /// 可能是int类型（分钟） 不能大于1440
        /// </summary>
        public int Cron { get; set; }
    }
}
