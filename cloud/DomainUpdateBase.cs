using ddns.net.model;
using ddns.net.service;

namespace ddns.net.cloud
{
    public abstract class DomainUpdateBase
    {
        public abstract Task<DomainResult> UpdateDomainRecord(DomainConfigInfo config, string Ip);


        public  DomainResult CheckConfigError (DomainConfigInfo _config,DomainResult result, string clientName)
        {
            result.ErrorMsg = $"{_config.DomainServer} DomainConfig is null or {clientName} init fail";
            Serilog.Log.Error(result.ErrorMsg);
            result.Error = true;
            return result;
        }

        public void AddDomainIpUnchanged(DomainConfigInfo _config,string Ip, DomainResult result,string SubDomain)
        {
            Serilog.Log.Debug($"{_config.DomainServer} recordIp = current GetIp ={Ip}");
            result.results.Add(new UpdateDomainRecordResult(SubDomain,Success: false, Error: $"{_config.DomainServer} {SubDomain}.{_config.Domain} recordIp = current GetIp ={Ip}", IsChanged: false));
        }

        public  void AddNewRecordResult(DomainConfigInfo _config, string Ip, DomainResult result, string subName, bool succ)
        {
            Serilog.Log.Debug($"{_config.DomainServer} AddRecord  {subName}  {(succ ? "success" : "fail")} Ip={Ip}");
            result.results.Add(new UpdateDomainRecordResult(subName,Success: succ, Error: succ ? "" : "fail", IsChanged: true));
        }

        public void AddUpdateRecordResult(DomainConfigInfo _config, DomainResult result,string subName,bool succ,string Ip)
        {
            Serilog.Log.Debug($"{_config.DomainServer} UpdateRecord  {subName}  {(succ ? "success" : "fail")} Ip={Ip}");
            result.results.Add(new UpdateDomainRecordResult(subName,Success: succ, Error: succ ? "" : $"{_config.DomainServer} update {subName} domainrecord fail", IsChanged: true));
        }

        public DomainResult ExceptionResult(DomainConfigInfo _config, DomainResult result, Exception ex)
        {
            var error = $"{_config.DomainServer} UpdateDomainRecord  error , {ex.Message}";
            Serilog.Log.Error(error);
            result.Error = true;
            result.ErrorMsg = error;
            return result;
        }
    }
}
