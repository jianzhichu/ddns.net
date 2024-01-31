using Newtonsoft.Json;
using Quartz;
using ddns.net.service;
using ddns.net.model;
using ddns.net.cloud.cz88;

namespace ddns.net.job
{
    /// <summary>
    /// 
    /// </summary>
    [DisallowConcurrentExecution]
    public class AutoDDNSJob : IJob
    {
        private readonly SqliteDbService  sqliteDbService;
        //private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IDomainService _domainService;
        private readonly IpHttpClient _ipHttpClient;

        public AutoDDNSJob(IDomainService domainService, IpHttpClient ipHttp, SqliteDbService db/*, IWebHostEnvironment webHostEnvironment*/)
        {
            _domainService = domainService;
            _ipHttpClient = ipHttp;
            sqliteDbService = db;
            //this.webHostEnvironment = webHostEnvironment;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Serilog.Log.Debug($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  The task is  being executed");
            //if(webHostEnvironment.IsDevelopment())
            //{
            //     await Task.CompletedTask;
            //}
            //return;
            try
            {

                var ipinfo = await _ipHttpClient.HttpGetIpInfo();

                if (ipinfo != null)
                {
                    Serilog.Log.Debug($" get ip from zc88 {JsonConvert.SerializeObject(ipinfo)}");
                    var lastLocalRecord = await sqliteDbService.GetLastDomainRecord();
                    if (lastLocalRecord != null && lastLocalRecord.Ip == ipinfo.ip)
                    {
                        Serilog.Log.Debug($" ip from zc88 = ip from db records");
                    }
                    else
                    {
                        var config =await sqliteDbService.GetDomainConfig();

                        if (config is null)
                        {
                            Serilog.Log.Error($"domainconfig is null");
                            await Task.CompletedTask;
                            return;
                        }
                        var stmpConfig = await sqliteDbService.GetStmpConfig();

                        var updateResult = await _domainService.UpdateDomainRecord(ipinfo.ip, config, stmpConfig);

                        if (updateResult != null)
                        {
                            if (updateResult.Error)
                            {
                                Serilog.Log.Debug($"UpdateDomainRecord IP = {ipinfo.ip}   error ={updateResult.ErrorMsg}");
                            }
                            else
                            {
                                if (updateResult.results != null)
                                {
                                    foreach (var result in updateResult.results)
                                    {
                                        if (result.IsChanged)
                                        {
                                            var insert = await sqliteDbService.InsertDomainRecord(new DomainRecordInfo
                                            {
                                                Address = $"{ipinfo.country}.{ipinfo.province}.{ipinfo.city}",
                                                Ip = ipinfo.ip,
                                                ISP = ipinfo.isp,
                                                LastIp = lastLocalRecord?.Ip,
                                                Servr = config?.DomainServer,
                                                MainDomain = $"{result.SubDomain}.{config.Domain}"
                                            });
                                            Serilog.Log.Debug($" Insertable user_client_info {result.SubDomain}.{config.Domain} ={(insert ? "success" : "fail")}");
                                        }
                                        else
                                        {
                                            Serilog.Log.Debug($"{result.SubDomain}.{config.Domain} dns ip={ipinfo.ip} not changed");
                                        }
                                    }
                                }
                                else
                                {
                                    Serilog.Log.Debug($"UpdateDomainRecord IP = {ipinfo.ip}   error：updateResult.results==null");
                                }
                            }
                        }
                        else
                        {
                            Serilog.Log.Debug($"UpdateDomainRecord IP = {ipinfo.ip}   result =null");
                        }
                    }
                }
                else
                {
                    Serilog.Log.Error($" get ip from  zc88 is null");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($" ddns job execute error {ex.StackTrace}");
            }
        }
    }
}
