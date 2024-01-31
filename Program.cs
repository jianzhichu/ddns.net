using ddns.net.extension;
using System.Text;
using Serilog;
using ddns.net.service;

namespace ddns.net
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder();

            builder.AddSerilog();
            builder.Services.AddControllers();
            builder.AddMyService();
            builder.AddMyJwt();


            var app = builder.Build();
            await app.Services.GetService<DDnsTimer>().StartAsync(CancellationToken.None);

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMySpa(builder);
            app.MapControllers();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            Log.Debug("ddns.net service is start success");
            app.Run();
        }


    }
}