using ddns.net.cloud.cz88;
using ddns.net.model;
using Newtonsoft.Json;

namespace ddns.net.service
{

    public class DDnsTimer
    {
        private Task _executingTask;
        private CancellationTokenSource _cts;
        private readonly SqliteDbService sqliteDbService;
        private readonly DomainService _domainService;
        private readonly IpHttpClient _ipHttpClient;


        public DDnsTimer(DomainService domainService, IpHttpClient ipHttp, SqliteDbService db)
        {
            _domainService = domainService;
            _ipHttpClient = ipHttp;
            sqliteDbService = db;
        }

        public void Dispose()
        {
            _executingTask?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Serilog.Log.Debug("ddns service is starting.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_cts.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
                return;

            Serilog.Log.Debug("ddns service is stopping.");

            _cts.Cancel();

            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();
        }

        protected virtual async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(10 * 1000, cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                Serilog.Log.Debug("ddns service is running.");

                // 执行后台任务逻辑
                var config = await sqliteDbService.GetDomainConfig();
                int cron = 10;
                if (config != null)
                {
                    cron= config.Cron;

                    Serilog.Log.Debug($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  The task is  being executed");
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
                else
                {

                    Serilog.Log.Error($"ddns configuration has not been initialized yet ");
                    await Task.CompletedTask;
                }
                await Task.Delay(TimeSpan.FromSeconds(cron), cancellationToken);
            }

            Serilog.Log.Debug("Background service has stopped.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task RestartService()
        {
            await StopAsync(CancellationToken.None);
            await StartAsync(CancellationToken.None);
        }
    }
}
