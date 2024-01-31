using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ddns.net.cloud.cz88;

namespace ddns.net.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DomainController : ControllerBase
    {
        private readonly SqliteDbService sqliteDbService;
        private readonly IpHttpClient _ipHttpClient;

        public DomainController(SqliteDbService sqlSugarClient, IpHttpClient ipHttpClient)
        {
            sqliteDbService = sqlSugarClient;
            _ipHttpClient = ipHttpClient;
        }

        [HttpGet("/success")]
        public IActionResult Index()
        {
            return Ok("service start success");
        }
        /// <summary>
        /// 获取解析变更记录
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetAnalysisRecords(RecordPageRequest recordPageRequest)
        {
            var page = await sqliteDbService.DomainRecordPgaeList(recordPageRequest);
            return Ok(new { code = 0, erro = "", data = page });
        }

        /// <summary>
        /// 查下当前网络公网IP
        /// </summary>
        /// <returns></returns>
        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetIp()
        {
            var data = await _ipHttpClient.HttpGetIpInfo();
            return Ok(data);
        }
       
    }
}
