using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ddns.net.dto;
using ddns.net.service;
using ddns.net.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;

namespace ddns.net.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class InitController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly SqliteDbService sqliteService;
        private readonly DDnsTimer dnsTimer;
        public InitController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment, SqliteDbService sqliteDbService, DDnsTimer  dnsTimer)
        {
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.sqliteService = sqliteDbService;
            this.dnsTimer = dnsTimer;
        }
        /// <summary>
        /// 检查是否已经初始化了
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Check()
        {
            var init = configuration["init"];
            var result = await Task.FromResult(new { code = init == "1" ? 0 : -1, erro = init == "1" ? "" : "uninitialized" });
            return Ok(result/*new { code=0}*/);
        }


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="systemInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Init(InitSystemInfo systemInfo)
        {

            try
            {
                var init = await sqliteService.Init(systemInfo);
                if (!init)
                {
                    return Ok(new { code = -1, erro = $"系统配置初始化失败," });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { code = -1, erro = $"系统配置初始化失败,{ex.Message}" });
            }

            try
            {
                Thread.Sleep(200);
                await SetInitAppsetting();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"UpdateAppsetting error,{ex.Message}");
            }

            try
            {
                Thread.Sleep(200);
                await dnsTimer.RestartService();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"StartDDNSJob error,{ex.Message}");
            }

            return Ok(new { code = 0, erro = "" });
        }

        /// <summary>
        /// 保存解析配置文件
        /// </summary>
        /// <param name="domainConfigInfo"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveDomainConfig(DomainConfigInfo domainConfigInfo)
        {
            var (code, erro) = await sqliteService.SaveDomainConfig(domainConfigInfo);
            if (code == 0)
            {
                await sqliteService.DeleteDomainRecords();//删除原来的解析记录--修改状态为isdelete=true
                try
                {
                    Thread.Sleep(200);
                    await dnsTimer.RestartService();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error($"StartDDNSJob error,{ex.Message}");
                }
                var start = 1;
                return Ok(new { code, erro, start });
            }
            else
            {
                return Ok(new { code, erro, start = false });
            }
        }

        /// <summary>
        /// 修改配置文件
        /// </summary>
        /// <returns></returns>
        private async Task SetInitAppsetting()
        {
            var jsonConfig = new { init = 1 };
            var jsonConfigString = JsonConvert.SerializeObject(jsonConfig, Formatting.Indented);
            string? configPath;
            if (webHostEnvironment.IsProduction())
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            }
            else
            {
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            }
            await System.IO.File.WriteAllTextAsync(configPath, jsonConfigString);

        }
    }


}
