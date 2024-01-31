using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ddns.net.extension;
using ddns.net.model;
using ddns.net.service;
using Newtonsoft.Json;

namespace ddns.net.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SysController : ControllerBase
    {
        private readonly SqliteDbService sqliteDbService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public SysController(SqliteDbService  sqliteDbService, IWebHostEnvironment webHostEnvironment)
        {
            this.sqliteDbService = sqliteDbService;
            this.webHostEnvironment = webHostEnvironment;
        }


        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePwd(UpdatePwdRequest user)
        {
            var (code, erro) = await sqliteDbService.UpdatePwd(user);
            return Ok(new { code, erro });
        }


        /// <summary>
        /// 获取解析配置文件
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetConfig()
        {
            var config =await sqliteDbService.GetDomainConfig();
            return Ok(new { code = 0, erro = "", data = config });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUserAvatar()
        {
            var user = await sqliteDbService.GetUser();
            return Ok(new { code = 0, error = "", data = new { user?.Avatar, user?.Id } });
        }

        /// <summary>
        /// 修改用户头像
        /// </summary>
        /// <param name="file"></param>
        /// <param name="Uid"></param>
        /// <returns></returns>
        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateUserAvatar(IFormFile file, [FromQuery] string Uid)
        {
            if (string.IsNullOrEmpty(Uid))
            {
                return Ok(new { code = -1, erro = "id不能为空" });
            }
            if (file != null && file.Length > 0)
            {
                long maxFileSize = 5 * 1024 * 1024; // 限制文件大小为5MB
                if (file.Length > maxFileSize)
                {
                    return Ok(new { code = -1, erro = "文件最大只能上传5M" });
                }
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = webHostEnvironment.IsProduction() ?
                    Path.Combine(Util.UPLOAD_PATH_PRO, fileName) : Path.Combine(Util.UPLOAD_PATH_DEV, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);

                    try
                    {
                        // 问了节约空间，删除文件夹下的所有文件
                        string[] files = Directory.GetFiles(webHostEnvironment.IsProduction() ? Util.UPLOAD_PATH_PRO : Util.UPLOAD_PATH_DEV);
                        foreach (string mfile in files)
                        {
                            if (!mfile.Contains(fileName))
                                System.IO.File.Delete(mfile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error($"delete file error ,{ex.Message}");
                    }
                    var update = await sqliteDbService.UpdateAvatar(Uid, fileName);

                    return Ok(new { code = update? 0:-1, erro = update? "":"上传失败", data = update?fileName:"" });
                }
            }
            else
            {
                return Ok(new { code = -1, erro = "空文件" });
            }
        }

     

        /// <summary>
        /// 修改邮件配置
        /// </summary>
        /// <param name="stmpConfig"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveStmpConfig(StmpConfigInfo stmpConfig)
        {
            var res = await sqliteDbService.SaveStmpConfig(stmpConfig);
            return Ok(new { code = res ? 0 : -1, erro = res ? "" : "修改失败" });

        }

        /// <summary>
        /// 邮件发送测试
        /// </summary>
        /// <returns></returns>
        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> SendEmail(string content= "hello world")
        {

            var config = await sqliteDbService.GetStmpConfig();
            if (config is null)
                return Ok("no stmp config");
            await MimeKitEmailServic.SendAsync(config, content);
            return Ok();
        }
        /// <summary>
        /// 修改邮件配置
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetStmpConfig()
        {
            var res = await sqliteDbService.GetStmpConfig();
            return Ok(new { code = res != null ? 0 : -1, erro = res != null ? "" : "没有配置", data = res });

        }


        /// <summary>
        /// 登录获取token
        /// </summary>
        /// <param name="loginUserInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest loginUserInfo)
        {
            if (loginUserInfo == null)
            {
                return Ok(new { code = -1, erro = "参数不能为空" });
            }
            else
            {
                var user = await sqliteDbService.GetUser(loginUserInfo.UserName);

                if (user == null)
                {
                    return Ok(new { code = -1, erro = "用户名或密码不正确" });
                }
                else
                {
                    if (user.Password == Util.Md5(loginUserInfo.Password))
                    {

                        var tokenString = GenerateJwtToken(user.UserName);

                        return Ok(new { code = 0, erro = "", token = tokenString, expires = 24 * 60 * 60 * 1000 });
                    }
                    else
                    {
                        return Ok(new { code = -1, erro = "用户名或密码不正确" });
                    }
                }
            }
        }

        /// <summary>
        /// reset,swagger里面不显示
        /// </summary>
        /// <returns></returns>
        //[HiddenApi]
        [HttpPost]
        public async Task<IActionResult> Reset()
        {
            Serilog.Log.Error($"危险操作，你正在重置所有配置，该操作将会清除所有配置表，请先进行备份，请谨慎谨慎再谨慎");

           var reset=await sqliteDbService.ResetAllConfigTable();


            var jsonConfig = new {  };
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

            return Ok(new { code = reset ? 0 : -1, erro = reset ? "成功" : "失败" });
        }


        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
            var k = webHostEnvironment.IsDevelopment() ? Util.JWT_TOKEN_KEY_DEV : Util.JWT_TOKEN_KEY;
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(k));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.Now.AddDays(1);

            var token = new JwtSecurityToken(
                issuer:Guid.NewGuid().ToString(),
                audience: Guid.NewGuid().ToString(),
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            return jwtToken;
        }
    }
}
