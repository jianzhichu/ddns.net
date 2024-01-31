using Newtonsoft.Json;
using SqlSugar;
using JDCloudSDK.Core.Auth;
using JDCloudSDK.Core.Http;
using JDCloudSDK.Domainservice.Client;
using JDCloudSDK.Clouddnsservice.Client;
using ddns.net.model;
using JDCloudSDK.Domainservice.Apis;
using JDCloudSDK.Clouddnsservice.Apis;
using JDCloudSDK.Clouddnsservice.Model;
using ddns.net.service;

namespace ddns.net.cloud.jingdong
{
    public class JingDongDomainService:DomainUpdateBase
    {
        private static ClouddnsserviceClient JDDNSClient;
        private static DomainserviceClient JDDomainClient;
        private static DomainConfigInfo? _config;
        private readonly SqlSugarDBService _db;
        private static readonly string JD = "jingdong";
        //https://docs.jdcloud.com/cn/jd-cloud-dns/api/describedomains

        public JingDongDomainService(SqlSugarDBService sqlSugarDBService)
        {

            _db = sqlSugarDBService;
            _config = sqlSugarDBService.GetDomainConfig();

            if (_config != null && _config.DomainServer == JD)
            {
                CredentialsProvider credentialsProvider = new StaticCredentialsProvider(_config.AK, _config.SK);
                JDDNSClient = new ClouddnsserviceClient.DefaultBuilder()
                         .CredentialsProvider(credentialsProvider)
                         .HttpRequestConfig(new HttpRequestConfig(Protocol.HTTPS))
                         .Build();
                JDDomainClient = new DomainserviceClient.DefaultBuilder()
                     .CredentialsProvider(credentialsProvider)
                     .HttpRequestConfig(new HttpRequestConfig(Protocol.HTTPS))
                     .Build();
            }
        }

        #region 京东云域名解析相关API


        #region 根据传入参数修改解析记录
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Ip"></param>
        /// <returns></returns>
        public override async Task<DomainResult> UpdateDomainRecord(string Ip)
        {
            var result = new DomainResult();

            try
            {
                if (_config == null || JDDNSClient == null)
                {
                    return CheckConfigError(_config,result, "JDDNSClient");
                }
                var subDomains = _config.SubDomain.Split(";");
                var recordIds = await _db.GetDomainRecordIds(_config.DomainServer);

                foreach (var subName in subDomains)
                {
                    if (string.IsNullOrEmpty(subName))
                        continue;

                    var domainId = await GetDomainId(subName, recordIds);
                    if (string.IsNullOrEmpty(domainId))
                    {
                        var error = $" {_config.DomainServer} UpdateDomainRecord -GetDomainId {_config.Domain}  error";
                        Serilog.Log.Error(error);
                        result.results.Add(new UpdateDomainRecordResult(subName, false, error));
                        continue;
                    }

                    var recordFromJD = await DescribeDomainRecords(domainId,subName);
                    if (recordFromJD?.HostValue == Ip)
                    {
                        AddDomainIpUnchanged(_config, Ip, result, subName);
                        continue;
                    }

                    var record = recordIds.FirstOrDefault(x => x.SubDomain == subName && x.Domain == _config.Domain);

                    if (string.IsNullOrWhiteSpace(record?.RecodeId))
                    {
                        //没有recordId说明是第一次，新增解析
                        var succ = await AddRecord(subName, Ip, domainId);
                        AddNewRecordResult(_config, Ip, result, subName, succ);
                    }
                    else
                    {
                      var  succ = await UpdateRecord(Ip, domainId, record.RecodeId, subName);
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
        /// 京东更新解析
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="domainId"></param>
        /// <param name="recordId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateRecord(string Ip, string domainId, string recordId, string subName)
        {
            var request = new UpdateRRRequest
            {
                DomainId = domainId,
                RegionId = "sz",
                Req = new UpdateRR
                {
                    DomainName = _config.Domain,
                    HostRecord = subName,
                    HostValue = Ip,
                    Id = Convert.ToInt32(recordId),
                    Type = string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType,
                    ViewValue = -1,
                    Ttl = 600
                }
            };
            var updateRes = await JDDNSClient.UpdateRR(request);
            //Serilog.Log.Debug($"jingdong  UpdateDomainRecord result= {JsonConvert.SerializeObject(updateRes)}");
            if (updateRes != null && updateRes.Result != null)
            {
                Serilog.Log.Debug($"{_config.DomainServer} IP={Ip} UpdateRecord {subName} success");
                return true;
            }
            else
            {
                var UpdateError = $"{_config.DomainServer} IP={Ip} UpdateRecord  {subName}  fail {JsonConvert.SerializeObject(updateRes)}";
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
        async Task<RR> DescribeDomainRecords(string domainId,string subName)
        {
          

            var res = await JDDNSClient.SearchRR(
                  new SearchRRRequest
                  {
                      DomainId = domainId,
                      PageSize = 10,
                      PageNumber = 1,
                      RegionId = "sz"
                  });
            if (res != null && res.Result != null)
            {

                var describes = res.Result.DataList.Where(x => x.HostRecord == subName && x.Type == _config.RecordType);
                return describes.FirstOrDefault();
            }
            return null;
        }

        #endregion

        #region 根据传入参数添加解析记录
        async Task<bool> AddRecord( string subName, string Ip,string domainId)
        {
            var request = new AddRRRequest
            {
                DomainId = domainId,
                RegionId = "sz",
                Req = new AddRR
                {
                    HostRecord = subName,
                    HostValue = Ip,
                    Type = string.IsNullOrEmpty(_config.RecordType) ? "A" : _config.RecordType,
                    Ttl = 600,
                    ViewValue = -1
                }
            };
            var res = await JDDNSClient.AddRR(request);

            if (res != null && res.Result != null && res.Result.DataList.Id.HasValue && res.Result.DataList.Id.Value > 0)
            {
                Serilog.Log.Debug($"{_config.DomainServer} add ddns  {subName}  success");
                 await UpdateDomainConfig(res.Result.DataList.Id.Value.ToString(), domainId, subName);
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

        #region 查询域名ID
        /// <summary>
        /// 查询域名ID
        /// </summary>
        /// <returns></returns>
        static async Task<string> GetDomainId(string subDomain, List<DomainRecordIdInfo> domainRecordIds)
        {

            if(domainRecordIds!=null&& domainRecordIds.Any(x=>x.Domain==_config.Domain&&x.SubDomain==subDomain))
            {
                var record = domainRecordIds.FirstOrDefault(x => x.Domain == _config.Domain && x.SubDomain == subDomain);
                if (record != null && !string.IsNullOrEmpty(record.DomainId))
                    return record.DomainId;
            }

            var response = await JDDomainClient.DescribeDomains(new DescribeDomainsRequest
            {
                DomainName = _config.Domain,
                RegionId = "sz",
                PageNumber = 1,
                PageSize = 10,
                RegionIdValue = "1"
            });

            if (response != null && response.Result != null && response.Result.DataList != null && response.Result.DataList.Any())
            {
                return response.Result.DataList.FirstOrDefault()?.Id.Value.ToString();
            }
            return string.Empty;
        }
        #endregion


        #region 更新数据库解析配置

        /// <summary>
        /// 更新解析配置-recordId,DomainId
        /// </summary>
        /// <param name="RecordId"></param>
        /// <param name="DomainId"></param>
        /// <param name="subName"></param>
        /// <returns></returns>
        private async Task<bool> UpdateDomainConfig(string? RecordId, string DomainId,string subName)
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
                    SubDomain = subName,
                    Domain = _config.Domain,
                    Server = _config.DomainServer
                }, new[] { "RecodeId", "DomainId" }) ;
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
