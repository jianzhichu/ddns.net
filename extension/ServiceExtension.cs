using Serilog;
using Serilog.Events;
using Serilog.Filters;
using ddns.net.cloud.aliyun;
using ddns.net.cloud.baidu;
using ddns.net.cloud.godaddy;
using ddns.net.cloud.huawei;
using ddns.net.cloud.tencent;
using ddns.net.cloud.west;
using ddns.net.service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.FileProviders;
using ddns.net.cloud.cz88;
using Serilog.Formatting.Compact;

namespace ddns.net.extension
{
    public static class ServiceExtension
    {

        public static void AddSerilog(this WebApplicationBuilder builder)
        {
            builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            });

            builder.Host.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            });
            builder.ConfigureLogging();
            builder.Host.UseSerilog();
        }

        /// <summary>
        /// Serilog 日志拓展
        /// </summary>
        public static void ConfigureLogging(this WebApplicationBuilder builder)
        {
            string dateFile = "";// DateTime.Now.ToString("yyyyMMdd");

            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Is(LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .Filter.ByExcluding(e => e.Level == LogEventLevel.Information) // 排除Info级别的日志
                .Filter.ByExcluding(Matching.FromSource("Microsoft"))
                .Filter.ByExcluding(Matching.FromSource("Quartz"))
                .WriteTo.Console(new RenderedCompactJsonFormatter(), LogEventLevel.Debug)
                //.WriteTo.MySQL(connectionString: builder.Configuration.GetConnectionString("DbConnectionString"), tableName: "Logs") // 输出到数据库
                .WriteTo.Logger(configure => configure
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug)
                    .WriteTo.File(
                        $"logs/log-debug-{dateFile}.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
                //.WriteTo.Logger(configure => configure
                //    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                //    .WriteTo.File(
                //        $"logs/log-info-{dateFile}.txt",
                //        rollingInterval: RollingInterval.Day,
                //        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
                .WriteTo.Logger(configure => configure
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
                    .WriteTo.File(
                        $"logs/log-error-{dateFile}.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
                //.WriteTo.File(
                //    $"logs/log-total-{dateFile}.txt",
                //    rollingInterval: RollingInterval.Day,
                //    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                //    restrictedToMinimumLevel: LogEventLevel.Debug)
                .CreateLogger();
        }

        public static void UseMySpa(this WebApplication app, WebApplicationBuilder builder)
        {
            var IsDev = builder.Environment.IsDevelopment();

            var uploadPath = IsDev ? Util.UPLOAD_PATH_DEV : Util.UPLOAD_PATH_PRO;
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(uploadPath),
                RequestPath = "/upload"
            });
            if (!IsDev)
            {
                app.UseSpaStaticFiles();
                app.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "app/";
                    // 指定默认路由
                    //spa.Options.DefaultPage = "/index.html";
                });
            }
        }

        public static void AddMyJwt(this WebApplicationBuilder builder)
        {

            var IsDev = builder.Environment.IsDevelopment();
            var k = IsDev ? Util.JWT_TOKEN_KEY_DEV : Util.JWT_TOKEN_KEY;

            var key = Encoding.ASCII.GetBytes(k);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                        return Task.CompletedTask;
                    }
                };
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(60)
                };
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public static void AddMyService(this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            //sqlite
            services.AddTransient<SqliteDbService>();
            services.AddTransient<DomainService>();
            //services.AddHostedService<DDnsTimer>();
            services.AddSingleton<DDnsTimer>();
            services.AddHttpClient();
            services.AddTransient<IpHttpClient>();

            services.AddTransient<AliyunHttpClient>();
            services.AddTransient<AliyunDomainService>();

            services.AddTransient<HuaweiHttpClient>();
            services.AddTransient<HuaweiSigner>();
            services.AddTransient<HuaweiDomainService>();

            services.AddTransient<TencentHttpClient>();
            services.AddTransient<TencentDomainService>();

            services.AddTransient<BaiduHttpClient>();
            services.AddTransient<BaiduDomainService>();

            services.AddTransient<GodaddyHttpClient>();
            services.AddTransient<GodaddyDomainService>();

            //services.AddTransient<JingDongDomainService>();

            services.AddTransient<WestDigitalHttpClient>();
            services.AddTransient<WestDigitalDomainService>();


          


            //加入spa，将前端vue项目一并build
            services.AddSpaStaticFiles(options =>
            {
                // In production, the UI files will be served from this directory
                options.RootPath = "app/dist";
            });
        }

    }
}