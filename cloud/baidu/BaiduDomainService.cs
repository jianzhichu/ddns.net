using Newtonsoft.Json;
using ddns.net.model;
using ddns.net.service;

namespace ddns.net.cloud.baidu
{
    /// <summary>
    /// 
    /// </summary>
    public class BaiduDomainService : DomainUpdateBase
    {
        private readonly BaiduHttpClient? BaiduClient;
        private  DomainConfigInfo? _config;
        private readonly SqliteDbService _db;
        //https://cloud.baidu.com/doc/BCD/s/4jwvymhs7#删除域名解析记录

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlSugarDBService"></param>
        /// <param name="baiduHttpClient"></param>
        public BaiduDomainService(SqliteDbService sqlSugarDBService, BaiduHttpClient baiduHttpClient)
        {
            _db = sqlSugarDBService;
            BaiduClient = baiduHttpClient;
        }

        #region 修改解析记录
        /// <summary>
        /// 新增或更新解析记录
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Ip"></param>
        /// <returns></returns>
        public override async Task<DomainResult> UpdateDomainRecord(DomainConfigInfo config,string Ip)
        {
            _config = config;
            var result = new DomainResult();
            try
            {
                if (_config == null || BaiduClient == null)
                {
                    return CheckConfigError(_config, result, "BaiduClient");
                }

                var subDomains = _config.SubDomain.Split(";");
                var recordIds = await _db.GetDomainRecordIds(_config.DomainServer);

                foreach (var subName in subDomains)
                {

                    if (string.IsNullOrEmpty(subName))
                        continue;
                    var recordFromBaidu = await DescribeDomainRecord(subName);
                    if (recordFromBaidu?.rdata == Ip)
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
                        var succ = await UpdateRecord(Ip, record?.RecodeId, subName);
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
        /// 百度云更新解析
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="recordId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateRecord(string Ip, string recordId, string subName)
        {
            var request = new BaiduEditRecordRequest
            {
                rdata = Ip,
                recordId = recordId,
                domain = subName,
                rdType = _config.RecordType,
                zoneName = _config.Domain
            };
            var updateRes = await BaiduClient.UpdateRecord(request, _config);
            if (updateRes)
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
        /// <summary>
        /// 根据传入参数获取指定主域名的所有解析记录列表
        /// </summary>
        /// <returns></returns>
        async Task<BaiduRecord> DescribeDomainRecord(string subName)
        {
            var res = await BaiduClient.GetRecords(_config.Domain, _config);
            if (res != null)
            {
                return res.result.FirstOrDefault(x =>
                x.domain == subName &&
                x.zoneName == _config.Domain &&
                x.rdtype == _config.RecordType);
            }
            return null;
        }

        #endregion

        #region 添加解析记录
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        async Task<bool> AddRecord(string Ip, string subName)
        {
            var res = await BaiduClient.AddRecord(new BaiduAddRecordRequest
            {
                rdata = Ip,
                domain = subName,
                rdType = string.IsNullOrWhiteSpace(_config.RecordType) ? "A" : _config.RecordType,
                zoneName = _config.Domain,
            }, _config);
            //Serilog.Log.Debug($"{_config.DomainServer} add ddns {(res ? "success" : "fail")} IP={Ip}");
            if (res)
            {
                try
                {
                    var recordId = string.Empty;
                    var records = await BaiduClient.GetRecords(_config.Domain, _config);
                    if (records != null)
                    {
                        recordId = records.result.FirstOrDefault(x =>
                            x.domain == subName &&
                            x.zoneName == _config.Domain &&
                            x.rdtype == _config.RecordType)?.recordId.ToString();
                    }
                    return await UpdateDomainConfig(recordId, subName);
                }
                catch (Exception ex)
                {
                    var UpdateError = $"{_config.DomainServer} UpdateDomainConfig error {ex.StackTrace}";
                    Serilog.Log.Error(UpdateError);
                    return false;
                }
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} Http AddRecord fail {JsonConvert.SerializeObject(res)}";
                Serilog.Log.Error(UpdateError);
                return false;
            }
        }

        #endregion



        #region 更新数据库解析配置

        /// <summary>
        /// 更新解析配置-recordId
        /// </summary>
        /// <param name="RecordId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateDomainConfig(string? RecordId, string subName)
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
                    RecodeId = RecordId,
                    Domain = _config.Domain,
                    SubDomain = subName,
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
    }
}
