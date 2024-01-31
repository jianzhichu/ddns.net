using Newtonsoft.Json;
using ddns.net.extension;
using ddns.net.model;
using ddns.net.service;

namespace ddns.net.cloud.west
{
    public class WestDigitalDomainService : DomainUpdateBase
    {
        private readonly WestDigitalHttpClient WestClient;
        private  DomainConfigInfo? _config;
        private readonly SqliteDbService _db;

        public WestDigitalDomainService(SqliteDbService sqlSugarDBService, WestDigitalHttpClient westClient)
        {
            _db = sqlSugarDBService;
            WestClient = westClient;
            if (!string.IsNullOrWhiteSpace(_config.SK))
            {
                //帐号认证
                WestClient.API = $"{WestClient.API}?username={_config.AK}&apikey={_config.SK.Md5()}";
            }
            else
            {
                //域名认证
                WestClient.API = $"{WestClient.API}?apidomainkey={_config.AK}";
            }
        }


        #region 根据传入参数修改解析记录
        /// <summary>
        /// 修改解析记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <returns></returns>
        public override async Task<DomainResult> UpdateDomainRecord(DomainConfigInfo config,string Ip)
        {
            _config = config;
            var result = new DomainResult();
            if (_config == null || WestClient == null)
            {
                return CheckConfigError(_config, result, "WestClient");
            }
            try
            {

                var subDomains = _config.SubDomain.Split(";");
                var recordIds = await _db.GetDomainRecordIds(_config.DomainServer);

                foreach (var subName in subDomains)
                {
                    if (string.IsNullOrEmpty(subName))
                        continue;
                    var recordFromWest = await DescribeDomainRecord(subName);
                    if (recordFromWest?.record_value == Ip)
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
                        var succ = await UpdateRecord(Ip, record.RecodeId, subName);
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
        /// 西部数码更新解析记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="recordId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateRecord(string Ip, string recordId, string subName)
        {
            var request = new WestDigitalRequest
            {
                domain = _config.Domain,
                record_id = recordId,
                hostname = subName,
                record_value = Ip,
                record_type = string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType
            };
            var updateRes = await WestClient.UpdateRecord(request);
            if (updateRes)
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord {subName} success");
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} IP={Ip} UpdateRecord {subName} fail {JsonConvert.SerializeObject(updateRes)}";
                Serilog.Log.Error(UpdateError);
                return false;
            }
        }

        #endregion

        #region 根据传入参数获取指定主域名的所有解析记录列表
        /// <summary>
        /// 根据传入参数获取指定主域名的所有解析记录列表
        /// </summary>
        /// <returns></returns>
        async Task<WestDigitalItem> DescribeDomainRecord(string subName)
        {
            var res = await WestClient.GetRecords(new WestDigitalRequest
            {
                domain = _config.Domain,
                hostname = subName,
                record_type = string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType
            });
            if (res != null)
            {
                return res.FirstOrDefault(x => x.hostname == subName &&
                x.record_type == (string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType));
            }
            return null;
        }

        #endregion

        #region 根据传入参数添加解析记录
        async Task<bool> AddRecord(string Ip, string subName)
        {
            var record_Id = await WestClient.AddRecord(new WestDigitalRequest
            {
                domain = _config.Domain,
                hostname = subName,
                record_type = string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType,
                record_value = Ip,
            });

            if (record_Id > 0)
            {
                await UpdateDomainConfig(record_Id.ToString(), subName);
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} Http AddRecord {subName}  fail ";
                Serilog.Log.Error(UpdateError);
                return false;
            }
        }



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
                    Id = _config.Id,
                    RecodeId = RecordId,
                    Domain = _config.Domain,
                    SubDomain = subName,
                }, new[] { "RecodeId" });
                Serilog.Log.Debug($"{_config.DomainServer}  update  config {(ins ? "success" : "fail")}");
                return ins;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"{_config.DomainServer} UpdateDomainConfig error {ex.Message},{ex.ToString}");
                return false;
            }
        }

        #endregion


        #endregion
    }
}
