using Newtonsoft.Json;
using ddns.net.model;
using ddns.net.service;

namespace ddns.net.cloud.tencent
{
    public class TencentDomainService : DomainUpdateBase
    {
        private  TencentHttpClient tencentClient;
        private  DomainConfigInfo? _config;
        private readonly SqliteDbService _db;

        //https://console.cloud.tencent.com/api/explorer?Product=dnspod&Version=2021-03-23&Action=CreateRecord
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlSugarDBService"></param>
        /// <param name="tencentHttpClient"></param>
        public TencentDomainService(SqliteDbService sqlSugarDBService,TencentHttpClient tencentHttpClient)
        {
            _db = sqlSugarDBService;
            this.tencentClient= tencentHttpClient;
        }

        #region 腾讯云域名解析相关API


        #region 根据传入参数修改解析记录
        /// <summary>
        /// 修改解析记录
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Ip"></param>
        /// <returns></returns>
        public override async Task<DomainResult> UpdateDomainRecord(DomainConfigInfo config, string Ip)
        {
            _config = config;
            var result = new DomainResult();
            if (_config == null || tencentClient == null)
            {
                return CheckConfigError(_config, result, "TencentClient");
            }

            try
            {
                var subDomains = _config.SubDomain.Split(";");
                var recordIds = await _db.GetDomainRecordIds(_config.DomainServer);

                foreach (var subName in subDomains)
                {
                    if (string.IsNullOrEmpty(subName))
                        continue;

                    var domainId = await GetDomainId(subName, recordIds);
                    if (string.IsNullOrEmpty(domainId))
                    {
                        var error = $" {_config.DomainServer} UpdateDomainRecord -GetDomainId {subName} {_config.Domain}  error";
                        Serilog.Log.Error(error);
                        result.results.Add(new UpdateDomainRecordResult(subName, false, error));
                        continue;
                    }


                    var recordFromTencent = await DescribeDomainRecords(domainId, subName);
                    if (recordFromTencent?.FirstOrDefault()?.Value == Ip)
                    {
                        AddDomainIpUnchanged(_config, Ip, result, subName);
                        continue;
                    }
                    var record = recordIds.FirstOrDefault(x => x.SubDomain == subName && x.Domain == _config.Domain);

                    if (string.IsNullOrWhiteSpace(record?.RecodeId))
                    {
                        //没有recordId说明是第一次，新增解析
                        var succ = await AddRecord(Ip, domainId, subName);
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
        /// 腾讯更新解析记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="recordId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateRecord(string Ip, string recordId, string subName)
        {
            var request = new ModifyRecordRequest
            {
                RecordId = ulong.Parse(recordId),
                SubDomain = subName,
                RecordType = string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType,
                RecordLine = "默认",
                Value = Ip,
                Domain = _config.Domain
            };
            var updateRes = await tencentClient.Request<ModifyRecordRequest, CreateOrModifyRecordResponse>(_config, request, "ModifyRecord");
            if (updateRes != null && updateRes.RecordId.HasValue)
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord {subName}  success");
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

        #region 解析记录列表
        /// <summary>
        /// 根据传入参数获取指定主域名的所有解析记录列表
        /// </summary>
        /// <returns></returns>
         async Task<RecordListItem[]> DescribeDomainRecords(string domainId, string subName)
        {
            try
            {
                var res = await tencentClient.Request<DescribeRecordListRequest, DescribeRecordListResponse>(_config,
                 new DescribeRecordListRequest
                 {
                     Domain = _config.Domain,
                     DomainId = ulong.Parse(domainId),
                     Subdomain = subName,
                     RecordType = string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType
                 }, "DescribeRecordList");
                if (res != null && res.RecordList != null)
                {
                    return res.RecordList;
                }
                return null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($" DescribeDomainRecords errro ,{ex.StackTrace}");
                return null;
            }
        }

        #endregion

        #region 添加解析记录
        /// <summary>
        /// 添加解析记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="domainId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        async Task<bool> AddRecord(string Ip, string domainId, string subName)
        {
            var req = new CreateRecordRequest
            {
                Domain = _config.Domain,
                DomainId = ulong.Parse(domainId),
                SubDomain = subName,
                RecordType = string.IsNullOrWhiteSpace(_config.RecordType) ? "A" : _config.RecordType,
                RecordLine = "默认",
                Value = Ip
            };

            var res = await tencentClient.Request<CreateRecordRequest, CreateOrModifyRecordResponse>(_config, req, "CreateRecord");

            if (res != null && res.RecordId.HasValue)
            {
                Serilog.Log.Debug($"{_config.DomainServer} add ddns  {subName} success");
                await UpdateDomainConfig(res.RecordId.Value.ToString(), domainId, subName);
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} {JsonConvert.SerializeObject(res)}";
                Serilog.Log.Debug(UpdateError);
                return false;
            }
        }

        /// <summary>
        /// 获取DomainId
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetDomainId(string subDomain, List<DomainRecordIdInfo> domainRecordIds)
        {
            if (domainRecordIds != null && domainRecordIds.Any(x => x.Domain == _config.Domain && x.SubDomain == subDomain))
            {
                var record = domainRecordIds.FirstOrDefault(x => x.Domain == _config.Domain && x.SubDomain == subDomain);
                if (record != null && !string.IsNullOrEmpty(record.DomainId))
                    return record.DomainId;
            }
            var res = await tencentClient.Request<DescribeDomainRequest, DescribeDomainResponse>(_config, new DescribeDomainRequest { Domain = _config.Domain }, "DescribeDomain");
            return res?.DomainInfo?.DomainId?.ToString();
        }

        #endregion


        #region 更新数据库解析配置

        /// <summary>
        /// 更新解析配置-recordId
        /// </summary>
        /// <param name="RecordId"></param>
        /// <param name="DomainId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateDomainConfig(string? RecordId, string DomainId, string subName)
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
                    DomainId = DomainId,
                    Domain = _config.Domain,
                    SubDomain = subName,
                    Server = _config.DomainServer
                }, new[] { "RecodeId", "DomainId" });
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
