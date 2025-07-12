using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HeartbeatMonitoringAlert.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendAlert : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public string botToken { get; set; }

        public SendAlert(IConfiguration configuration)
        {
            _configuration = configuration;
            botToken = _configuration["TokenTelegram"];
        }

        [HttpPost("SendAlertTelegram")]
        public async Task<IActionResult> SendAlertTelegram([FromBody] AlertRequestTelegram alertRequest)
        {
            List<long> lsChatID = new List<long>();
            List<string> errorList = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(botToken))
                {
                    throw new Exception("Bot token is not set in environment variables.");
                }

                var botClient = new TelegramBotClient(botToken);
                
                //testing bot
                var me = await botClient.GetMeAsync();
                Console.WriteLine($"Username BOT: {me.Username}");

                // set label
                string ipLabel = "IP / URL";
                string statusLabel = "STATUS";
                string timeLabel = "TIME";
                string messageLabel = "MESSAGE";

                int longestLabelLength = Math.Max(ipLabel.Length, Math.Max(statusLabel.Length, Math.Max(timeLabel.Length, messageLabel.Length)));
                int padding = 5;  // set jumlah spasi

                var formatSendMessage = @$"[ALERT] - HEARTBEAT MONITORING SERVER - {alertRequest.ServerName}" +
                                         $"\n{ipLabel.PadRight(longestLabelLength + padding)}: {alertRequest.IPAddress}\n" +
                                         $"{statusLabel.PadRight(longestLabelLength + 3)}: {alertRequest.status}\n" +
                                         $"{timeLabel.PadRight(longestLabelLength + 6)}: {alertRequest.statusTime}\n" +
                                         $"{messageLabel.PadRight(longestLabelLength)}: {alertRequest.message}";
                string userID = "";
                try
                {
                    foreach (string sendChatID in alertRequest.IDTelegram.Split(";"))
                    {
                        userID = sendChatID;
                        try
                        {
                            Message message = await botClient.SendTextMessageAsync(
                                chatId: sendChatID,
                                text: formatSendMessage
                            );

                            Console.WriteLine($"Message sent to chat ID: {sendChatID}");
                        }
                        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
                        {
                            Console.WriteLine($"Error sending message to chat ID {sendChatID}: {ex.Message}");
                            errorList.Add($"User ID: {sendChatID}, Message: {ex.Message}");
                        }
                    }

                    if (errorList.Any())
                    {
                        return Ok(new { status = (int)HttpStatusCode.OK, errors = errorList });
                    }
                    else
                    {
                        return Ok(new { status = (int)HttpStatusCode.OK, message = "Alert sent successfully!" });
                    }
                }
                catch (Exception ex)
                {
                    return Ok(new { status = (int)HttpStatusCode.OK, userID = userID, error = ex.Message });
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Ok(new { status = (int)HttpStatusCode.NotFound, ex.Message });
            }
        }

        [HttpPost("SendAlertEmail")]
        public async Task<IActionResult> SendAlertEmail([FromBody] AlertRequestEmail alertRequest)
        {
            string SendEmailSwitch = _configuration["SendEmailSwitch"];
            string EmailTempPath = _configuration["EmailTempPath"];
            string EmailFrom = _configuration["EmailFrom"];
            string PassEmailFrom = _configuration["PassEmailFrom"];
            string HostEmail = _configuration["HostEmail"];

            try
            {
                // format body email
                StringBuilder sb = new StringBuilder();
                sb.Append("<label>[ALERT] HEARTBEAT MONITORING SERVER - " + alertRequest.ServerName + "<label/>");
                sb.Append("<table>");
                sb.AppendFormat("<tr><td>IP / URL<td><td>:<td><td>{0}<td></tr>", alertRequest.IPAddress);
                sb.AppendFormat("<tr><td>STATUS<td><td>:<td><td>{0}<td></tr>", alertRequest.status);
                sb.AppendFormat("<tr><td>TIME<td><td>:<td><td>{0}<td></tr>", alertRequest.statusTime);
                sb.AppendFormat("<tr><td>MESSAGE<td><td>:<td><td>{0}<td></tr>", alertRequest.message);
                sb.Append("</table>");
                string bodyEmail = sb.ToString();

                using (MailMessage msg = new MailMessage())
                {
                    msg.From = new MailAddress(EmailFrom);
                    msg.To.Add(alertRequest.EmailTo.Replace(";", ","));
                    msg.CC.Add(alertRequest.EmailCC.Replace(";", ","));
                    msg.Subject = $"[ALERT] HEARTBEAT MONITORING SERVER - {alertRequest.ServerName}";
                    msg.Body = bodyEmail;
                    msg.IsBodyHtml = true;

                    if (SendEmailSwitch.ToLower() == "on")
                    {
                        using (SmtpClient client = new SmtpClient(HostEmail, 587))
                        {
                            client.EnableSsl = true;
                            client.UseDefaultCredentials = false;
                            client.Credentials = new NetworkCredential(EmailFrom, PassEmailFrom);
                            client.Send(msg);
                        }
                    }
                    else
                    {
                        SmtpClient client = new SmtpClient();
                        client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                        client.PickupDirectoryLocation = EmailTempPath;
                        client.Send(msg);
                    }
                }

                return Ok(new { status = (int)HttpStatusCode.OK, message = "Alert sent successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Ok(new { status = (int)HttpStatusCode.NotFound, error = ex.Message });
            }
        }
    }
}
