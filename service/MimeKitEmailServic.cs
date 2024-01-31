using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using ddns.net.model;

namespace ddns.net.service
{
    public class MimeKitEmailServic
    {
       
        public static async Task SendAsync(StmpConfigInfo config,string content)
        {
            if(config is null || string.IsNullOrEmpty(content))
            {
                await Task.CompletedTask;
                return;
            }
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(config.From.Split("@")[0], config.From));
                message.To.Add(new MailboxAddress(config.To.Split("@")[0], config.To));
                message.Subject = "DDNS.NET-云解析解析记录修改通知";

                message.Body = new TextPart("plain")
                {
                    Text = content
                };

                using (var client = new SmtpClient())
                {
                    client.Connect(config.Stmp, config.Port, true);
                    //smtp.qq.com smtp 465.yeah.net 587
                    client.Authenticate(config.From, config.Code);

                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {

                Serilog.Log.Error($"send emial message error,{ex.Message}");
            }
        }
    }
}
