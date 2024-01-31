using ddns.net.cloud.aliyun;
using ddns.net.cloud.baidu;
using ddns.net.cloud.godaddy;
using ddns.net.cloud.huawei;
//using ddns.net.cloud.jingdong;
using ddns.net.cloud.tencent;
using ddns.net.cloud.west;
using ddns.net.model;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ddns.net.service
{
    public class DomainService 
    {
        private readonly IServiceProvider serviceProvider;


        public DomainService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        /// <summary>
        /// 更新记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="config"></param>
        /// <param name="stmpConfig"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task<DomainResult> UpdateDomainRecord(string Ip, DomainConfigInfo config, StmpConfigInfo stmpConfig)
        {
            try
            {
                if (config != null)
                {
                    var res = config.DomainServer switch
                    {
                        "aliyun" => await serviceProvider.GetService<AliyunDomainService>().UpdateDomainRecord(config,Ip),
                        "tencent" => await serviceProvider.GetService<TencentDomainService>().UpdateDomainRecord(config, Ip),
                        "huawei" => await serviceProvider.GetService<HuaweiDomainService>().UpdateDomainRecord(config, Ip),
                        "baidu" => await serviceProvider.GetService<BaiduDomainService>().UpdateDomainRecord(config, Ip),
                        "godaddy" => await serviceProvider.GetService<GodaddyDomainService>().UpdateDomainRecord(config, Ip),
                        //"jingdong" => await serviceProvider.GetService<JingDongDomainService>().UpdateDomainRecord(config,Ip),
                        "west" => await serviceProvider.GetService<WestDigitalDomainService>().UpdateDomainRecord(config, Ip),
                        _ => throw new NotSupportedException($"暂时不支持 {config.DomainServer} 解析"),
                    };


                    if (!res.Error)
                    {
                        var result = res.results;

                        try
                        {
                            if (result != null && result.Any(x => x.Success && x.IsChanged))
                            {
                                if (stmpConfig != null && stmpConfig.Open)
                                {
                                    stmpConfig.To = stmpConfig.From;//自己发自己
                                    var content = $" 您于{DateTime.Now:yyyy-MM-dd HH:mm:ss},通过DDNS.NET 对{config.DomainServer} 云DNS平台的域名\r\n" +
                                        $"{string.Join("\r\n", result.Where(x => x.Success && x.IsChanged).Select(x => $"{x.SubDomain}.{config.Domain} 解析修改为 {Ip} 结果：{x.Success}"))}";
                                    await MimeKitEmailServic.SendAsync(stmpConfig, content);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error($"发送邮件失败,{ex.Message}");
                            return res;
                        }
                    }
                    return res;
                }
                return new DomainResult() { Error = true, ErrorMsg = "没有找到域名解析配置" };
            }
            catch (Exception ex)
            {
                var errp = $"DomainService.UpdateDomainRecord,{ex.StackTrace},{ex.Message}";
                Serilog.Log.Error(errp);
                return new DomainResult() { Error = true, ErrorMsg = errp };
            }
        }
    }

    public class UpdateDomainRecordResult
    {
        public string SubDomain { get; set; }

        public bool Success { get; set; }

        public string Error { get; set; }

        public bool IsChanged { get; set; }


        public UpdateDomainRecordResult(string SubDomain, bool Success, string Error, bool IsChanged = true)
        {
            this.SubDomain = SubDomain;
            this.Success = Success;
            this.Error = Error;
            this.IsChanged = IsChanged;
        }
    }


    public class DomainResult
    {

        public List<UpdateDomainRecordResult> results { get; set; } = new List<UpdateDomainRecordResult>();

        public bool Error { get; set; }

        public string ErrorMsg { get; set; }
    }
}
