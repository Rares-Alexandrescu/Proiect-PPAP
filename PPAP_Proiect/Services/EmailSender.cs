using MailKit.Net.Smtp;
using MimeKit;

namespace PPAP_Proiect.Services
{
	public class EmailSender
	{
		private readonly string SMTPServer;
		private readonly int SMTPPort;
		private readonly string SMTPEmail;
		private readonly string SMTPPassword;

		public EmailSender(IConfiguration config)
		{
			SMTPServer = config.GetValue<string>("SMTPSettings:SMTPServer", "");
			SMTPPort = config.GetValue<int>("SMTPSettings:SMTPPort", 0);
			SMTPEmail = config.GetValue<string>("SMTPSettings:SMTPEmail", "");
			SMTPPassword = config.GetValue<string>("SMTPSettings:SMTPPassword", "");
		}

		public string getEmail()
		{
			return SMTPEmail;
		}

		public async Task SendEmailAsync(string toName, string toEmail, string subject, string body)
		{
			var message = new MimeMessage();

			message.From.Add(new MailboxAddress("PPAP", SMTPEmail));
			message.To.Add(new MailboxAddress(toName, toEmail));
			message.Subject = subject;

			message.Body = new TextPart("html")
			{
				Text = body
			};

			using (var client = new SmtpClient())
			{
				await client.ConnectAsync(SMTPServer, SMTPPort, false);
				await client.AuthenticateAsync(SMTPEmail, SMTPPassword);
				await client.SendAsync(message);
				await client.DisconnectAsync(true);
			}
		}
	}
}
