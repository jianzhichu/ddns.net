using Newtonsoft.Json;
using ddns.net.model;
using ddns.net.service;

namespace ddns.net.cloud.huawei
{
    /// <summary>
    /// 
    /// </summary>
    public class HuaweiDomainService:DomainUpdateBase
    {
        private  DomainConfigInfo? _config;
        private readonly SqliteDbService _db;
        private readonly HuaweiHttpClient _huaweiClient;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlSugarDBService"></param>
        public HuaweiDomainService(SqliteDbService sqlSugarDBService,HuaweiHttpClient huaweiClient)
        {
            _db = sqlSugarDBService;
            _huaweiClient = huaweiClient;
        }

        #region 华为云域名解析相关API

        #region 根据传入参数修改解析记录
        //https://console.huaweicloud.com/apiexplorer/#/openapi/DNS/doc?api=UpdateRecordSet
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Ip"></param>
        /// <returns></returns>
        public override async Task<DomainResult> UpdateDomainRecord(DomainConfigInfo config, string Ip)
        {
            _config = config;
            var result = new DomainResult();

            if (_config == null || _huaweiClient == null)
            {
                return CheckConfigError(_config, result, "HuaweiClient");
            }
            try
            {
                var subDomains = _config.SubDomain.Split(";");
                var recordIds = await _db.GetDomainRecordIds(_config.DomainServer);
                foreach (var subName in subDomains)
                {
                    if (string.IsNullOrEmpty(subName))
                        continue;

                    var zoneId = await GetDomainZoneId(subName, recordIds);
                    if (string.IsNullOrEmpty(zoneId))
                    {
                        var error = $" {_config.DomainServer} UpdateDomainRecord -GetDomainZoneId {_config.Domain}  error";
                        Serilog.Log.Error(error);
                        result.results.Add(new UpdateDomainRecordResult(subName,false, error));
                        continue;
                    }

                    var recordFromHuawei = await DescribeDomainRecord(subName, zoneId);
                    if (recordFromHuawei?.records?.FirstOrDefault() == Ip)
                    {
                        AddDomainIpUnchanged(_config, Ip, result, subName);
                        continue;
                    }
                    var record = recordIds.FirstOrDefault(x => x.SubDomain == subName && x.Domain == _config.Domain);
                    if (string.IsNullOrWhiteSpace(record?.RecodeId))
                    {
                        //没有recordId说明是第一次，新增解析
                       var succ = await AddRecord(Ip, zoneId, subName);
                        AddUpdateRecordResult(_config, result, subName, succ, Ip);
                    }
                    else
                    {
                      var  succ= await UpdateRecord(Ip,zoneId,subName,record.RecodeId);
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
        /// 华为更新解析记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="zoneId"></param>
        /// <param name="subName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        private  async Task<bool> UpdateRecord(string Ip, string zoneId, string subName, string recordId)
        {

            var updateRes = await _huaweiClient.UpdateRecordSetAsync(new UpdateRecordSetRequestBody
            {
                name = $"{subName}.{_config.Domain}.",
                type = string.IsNullOrWhiteSpace(_config.RecordType) ? "A" : _config.RecordType,
                records = new List<string> { Ip }
            }, _config, zoneId, recordId);
            if (updateRes != null && !string.IsNullOrEmpty(updateRes.id))
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord {subName} success");
                return true;
            }
            else
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord {subName} fail {JsonConvert.SerializeObject(updateRes)}");
                return false;
            }
        }

        #endregion

        #region 解析记录列表
        //https://console.huaweicloud.com/apiexplorer/#/openapi/DNS/sdk?api=ListRecordSetsByZone
        /// <summary>
        /// 根据传入参数获取指定主域名的所有解析记录列表
        /// </summary>
        /// <param name="subName">二级域名</param
        /// <returns></returns>
        async Task<ListRecordSets> DescribeDomainRecord(string subName, string zoneId)
        {
            var request = new ListRecordSetsByZoneRequest
            {
                name = $"{subName}.{_config.Domain}.",//华为云与腾讯跟阿里云的区别,
                type = string.IsNullOrWhiteSpace(_config.RecordType) ? "A" : _config.RecordType,
            };
            var res = await _huaweiClient.ListRecordSetsByZoneAsync(request , _config, zoneId);
            if (res != null && res.recordsets != null)
            {
                Serilog.Log.Debug($"{_config.DomainServer} DescribeDomainRecords success");
                var describes = res.recordsets.FirstOrDefault();
                if (describes != null)
                    return describes;

                var UpdateError = $"{_config.DomainServer} DescribeDomainRecords fail,{JsonConvert.SerializeObject(res)}";
                Serilog.Log.Error(UpdateError);
                return null;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} DescribeDomainRecords error,{JsonConvert.SerializeObject(res)}";
                Serilog.Log.Error(UpdateError);
                return null;
            }
        }

        #endregion

        #region 添加解析记录
        //https://console.huaweicloud.com/apiexplorer/#/openapi/DNS/debug?api=CreateRecordSet
        /// <summary>
        /// 添加解析记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="zoneId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        async Task<bool> AddRecord(string Ip, string zoneId, string subName)
        {
            var res = await _huaweiClient.CreateRecordSetAsync(new CreateRecordSetRequestBody
            {
                name = $"{subName}.{_config.Domain}.",
                type = string.IsNullOrWhiteSpace(_config.RecordType) ? "A" : _config.RecordType,
                records = new List<string> { Ip }
            }, _config, zoneId);

            if (res != null && !string.IsNullOrEmpty(res.id) && !string.IsNullOrEmpty(res.zone_id))
            {
                Serilog.Log.Debug($"{_config.DomainServer} add ddns {subName} success");
                await UpdateDomainConfig(res.id, res.zone_id, subName);
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} CreateRecordSetAsync {subName} error {JsonConvert.SerializeObject(res)}";
                Serilog.Log.Error(UpdateError);
                return false;
            }
        }

        #region 更新数据库解析配置

        /// <summary>
        /// 更新解析配置-recordId
        /// </summary>
        /// <param name="RecordId"></param>
        /// <param name="ZoneId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateDomainConfig(string? RecordId, string ZoneId, string subName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(RecordId))
                {
                    Serilog.Log.Debug($"{_config.DomainServer}  update  config error RecordId=null");
                    return false;
                }
                var ins = await _db.UpdateDomainRecordIdWithColums(new DomainRecordIdInfo
                {
                    Id = _config.Id,
                    RecodeId = RecordId,
                    ZoneId = ZoneId,
                    SubDomain = subName,
                    Domain = _config.Domain,
                    Server = _config.DomainServer
                }, new[] { "RecodeId", "ZoneId" });
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

        #region 查询公网Zone列表
        //https://console.huaweicloud.com/apiexplorer/#/openapi/DNS/sdk?api=ListPublicZones


        /// <summary>
        /// GetDomainZoneId
        /// </summary>
        /// <param name="subDomain"></param>
        /// <param name="domainRecordIds"></param>
        /// <returns></returns>
         async Task<string> GetDomainZoneId(string subDomain, List<DomainRecordIdInfo> domainRecordIds)
        {
            if (domainRecordIds != null && domainRecordIds.Any(x => x.Domain == _config.Domain && x.SubDomain == subDomain))
            {
                var record = domainRecordIds.FirstOrDefault(x => x.Domain == _config.Domain && x.SubDomain == subDomain);
                if (record != null && !string.IsNullOrEmpty(record.ZoneId))
                    return record.ZoneId;
            }
            var req = new ListPublicZonesRequest
            {
                name = _config.Domain
            };
            try
            {
                var resp = await _huaweiClient .ListPublicZonesAsync(req,_config);
                var ZoneId = resp?.Zones?.FirstOrDefault()?.Id;
                if (!string.IsNullOrEmpty(ZoneId))
                {
                    Serilog.Log.Debug($" GetDomainZoneId success, zoneId={ZoneId}");
                    return ZoneId;
                }
                else
                {
                    Serilog.Log.Error($" GetDomainZoneId fail, res={JsonConvert.SerializeObject(resp)}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"{_config.DomainServer} GetDomainZoneId error {ex.Message}");
                return null;
            }
        }
        #endregion
        #endregion
    }
}
