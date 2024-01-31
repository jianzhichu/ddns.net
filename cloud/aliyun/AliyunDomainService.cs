using Newtonsoft.Json;
using ddns.net.model;
using ddns.net.service;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ddns.net.cloud.aliyun
{
    /// <summary>
    /// 
    /// </summary>
    public class AliyunDomainService : DomainUpdateBase
    {
        private  DomainConfigInfo? _config;
        private readonly SqliteDbService sqliteDbService;

        private readonly AliyunHttpClient _aliClient;
        //https://api.aliyun.com/api/Alidns/2015-01-09/AddDomainRecord

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlSugarDBService"></param>
        /// <param name="aliyunHttpClient"></param>
        public AliyunDomainService(SqliteDbService  sqliteDbService, AliyunHttpClient aliyunHttpClient)
        {
            this.sqliteDbService = sqliteDbService;
            _aliClient = aliyunHttpClient;
        }

        #region 修改解析记录

        /// <summary>
        /// 更新解析记录
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Ip"></param>
        /// <returns></returns>
        public override async Task<DomainResult> UpdateDomainRecord(DomainConfigInfo config, string Ip)
        {
            _config = config;
            var result = new DomainResult();
            try
            {
                if (_config == null || _aliClient == null)
                {
                    return CheckConfigError(_config, result, "AliyunClient");
                }
                var subDomains = _config.SubDomain.Split(";");
                var recordIds = await sqliteDbService.GetDomainRecordIds(_config.DomainServer);

                foreach (var subName in subDomains)
                {
                    if (string.IsNullOrEmpty(subName))
                        continue;
                    var recordFromAliyun = await DescribeDomainRecord(subName);
                    if (recordFromAliyun?.Value == Ip)
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
        /// 阿里云更新解析记录
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="RecordId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateRecord(string Ip, string RecordId, string subName)
        {
            var request = new AliyunUpdateDomainRecordRequest
            {
                Action = "UpdateDomainRecord",
                RecordId = RecordId,
                RR = subName,
                Type = _config.RecordType,
                Value = Ip
            };
            var updateRes = await _aliClient.Request<AliyunUpdateDomainRecordRequest, AliyunDomainRecordResponse>(_config, request);
            if (updateRes != null)
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord {subName} success,{updateRes.RecordId}");
                return true;
            }
            else
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord  {subName} fail {JsonConvert.SerializeObject(updateRes)}");
                return false;
            }
        }

        #endregion

        #region 解析记录列表
        /// <summary>
        /// 根据传入参数获取指定主域名的所有解析记录列表
        /// </summary>
        /// <returns></returns>
        async Task<AliyunRecordItem> DescribeDomainRecord(string subName)
        {
            var res = await _aliClient.Request<AliyunDescribeSubDomainRecordsRequest, AliyunDescribeSubDomainRecordsResponse>(
                 _config, new AliyunDescribeSubDomainRecordsRequest
                 {
                     Action = "DescribeSubDomainRecords",
                     DomainName = _config.Domain,
                     Type = _config.RecordType,
                     SubDomain = $"{subName}.{_config.Domain}"
                 });
            if (res != null)
            {
                var record = res.DomainRecords?.Record?.FirstOrDefault();
                return record;
            }
            var UpdateError = $"{_config.DomainServer} DescribeSubDomainRecords  ERROR  {JsonConvert.SerializeObject(res)} ";
            Serilog.Log.Error(UpdateError);
            return null;
        }

        #endregion

        #region 添加解析记录
        async Task<bool> AddRecord(string Ip, string subName)
        {
            var request = new AliyunAddDomainRecordRequest
            {
                Action = "AddDomainRecord",
                DomainName = _config.Domain,
                RR = subName,
                Type = string.IsNullOrWhiteSpace(_config.RecordType) ? "A" : _config.RecordType,
                Value = Ip
            };
            var res = await _aliClient.Request<AliyunAddDomainRecordRequest, AliyunDomainRecordResponse>(_config, request);
            if (res != null)
            {
                Serilog.Log.Debug($"{_config.DomainServer} add ddns  {subName}  success");
                await UpdateDomainConfig(res?.RecordId, subName);
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer}  add ddns  {subName}  error {JsonConvert.SerializeObject(res)}";
                Serilog.Log.Debug(UpdateError);
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
                var ins = await sqliteDbService.UpdateDomainRecordIdWithColums(new DomainRecordIdInfo
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
