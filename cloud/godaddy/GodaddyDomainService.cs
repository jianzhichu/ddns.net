//using ClockSnowFlake;
using Newtonsoft.Json;
using ddns.net.model;
using ddns.net.service;

namespace ddns.net.cloud.godaddy
{
    /// <summary>
    /// 
    /// </summary>
    public class GodaddyDomainService : DomainUpdateBase
    {
        private readonly GodaddyHttpClient GodaddyClient;
        private  DomainConfigInfo? _config;
        private readonly SqliteDbService _db;
        //https://developer.godaddy.com/doc/endpoint/domains#/
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlSugarDBService"></param>
        /// <param name="godaddyHttpClient"></param>
        public GodaddyDomainService(SqliteDbService sqlSugarDBService, GodaddyHttpClient godaddyHttpClient)
        {
            _db = sqlSugarDBService;
            GodaddyClient = godaddyHttpClient;
        }


        #region 根据传入参数修改解析记录
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Ip"></param>
        /// <returns></returns>
        public override async Task<DomainResult> UpdateDomainRecord(DomainConfigInfo config,string Ip)
        {
            GodaddyClient.SK = config.SK;
            GodaddyClient.AK = config.AK;
            _config = config;
            var result = new DomainResult();

            if (_config == null || GodaddyClient == null)
            {
                return CheckConfigError(_config, result, "GodaddyClient");
            }
            try
            {
                var subDomains = _config.SubDomain.Split(";");
                var recordIds = await _db.GetDomainRecordIds(_config.DomainServer);

                foreach (var subName in subDomains)
                {
                    if (string.IsNullOrEmpty(subName))
                        continue;
                    var recordFromGodaddy = await DescribeDomainRecord(subName);
                    if (recordFromGodaddy?.data == Ip)
                    {
                        AddDomainIpUnchanged(_config, Ip, result, subName);
                        continue;
                    }
                    var record = recordIds.FirstOrDefault(x => x.SubDomain == subName && x.Domain == _config.Domain);
                    if (string.IsNullOrWhiteSpace(record?.RecodeId))
                    {
                        //没有recordId说明是第一次，新增解析
                        var succ = await AddRecord(Ip, subName);
                        AddNewRecordResult(_config, Ip, result, subName, succ);
                    }
                    else
                    {
                        var succ = await UpdateRecord(Ip, subName);
                        AddUpdateRecordResult(_config, result, subName, succ, Ip);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                return ExceptionResult(_config, result, ex);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateRecord(string Ip, string subName)
        {
            var request = new GodaddyEditRecordRequest
            {
                name = subName,
                domain = _config.Domain,
                type = string.IsNullOrWhiteSpace(_config.RecordType) ? "A" : _config.RecordType,
                records = new List<GodaddyEditRecordItem> { new() { data = Ip } }

            };
            var updateRes = await GodaddyClient.UpdateRecord(request);
            if (updateRes)
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord {subName} success");
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} IP={Ip} UpdateRecord {subName}  fail {JsonConvert.SerializeObject(updateRes)}";
                Serilog.Log.Debug(UpdateError);
                return false;
            }
        }

        #endregion

        #region 根据传入参数获取指定主域名的所有解析记录列表
        /// <summary>
        /// 根据传入参数获取指定主域名的所有解析记录列表
        /// </summary>
        /// <returns></returns>
        async Task<GodaddyRecord> DescribeDomainRecord(string subName)
        {
            var res = await GodaddyClient.GetRecords(_config.Domain, _config.RecordType, subName);
            if (res != null)
            {
                return res.FirstOrDefault(x => x.name == subName && x.type == _config.RecordType);
            }
            return null;
        }

        #endregion

        #region 根据传入参数添加解析记录
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        async Task<bool> AddRecord(string Ip, string subName)
        {
            var res = await GodaddyClient.AddRecord(new GodaddyAddRecordRequest
            {
                domain = _config.Domain,
                records = new List<GodaddyAddRecordItem> {
                            new() {
                                data=Ip,
                                name=subName,
                                type=string.IsNullOrEmpty(_config.RecordType)?"A":_config.RecordType
                            } },
            });

            if (res)
            {
                await UpdateDomainConfig(subName);
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} GodaddyClient AddRecord fail {JsonConvert.SerializeObject(res)}";
                Serilog.Log.Error(UpdateError);
                return false;
            }
        }

        #region 更新数据库解析配置

        /// <summary>
        /// 更新解析配置-recordId--godaddy么有recordid
        /// </summary>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateDomainConfig(string subName)
        {
            try
            {
                var ins = await _db.UpdateDomainRecordIdWithColums(new DomainRecordIdInfo
                {
                    RecodeId = "godaddy_" + Guid.NewGuid().ToString(),
                    SubDomain = subName,
                    Domain = _config.Domain,
                    Server = _config.DomainServer
                }, new[] { "RecodeId" });
                Serilog.Log.Debug($"{_config.DomainServer}  update  config {(ins ? "success" : "fail")}");

                return ins;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"{_config.DomainServer} UpdateDomainConfig error {ex.Message},{ex.StackTrace}");
                return false;
            }
        }

        #endregion



        #endregion
    }
}
